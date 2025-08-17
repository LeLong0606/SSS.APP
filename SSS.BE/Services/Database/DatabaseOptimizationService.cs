using Microsoft.EntityFrameworkCore;
using SSS.BE.Persistence;

namespace SSS.BE.Services.Database;

public interface IDatabaseOptimizationService
{
    Task OptimizeIndexesAsync();
    Task AnalyzeTableStatisticsAsync();
    Task CleanupOldDataAsync();
    Task CheckIndexUsageAsync();
    Task<DatabaseHealthReport> GetDatabaseHealthAsync();
}

public class DatabaseOptimizationService : IDatabaseOptimizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseOptimizationService> _logger;

    public DatabaseOptimizationService(ApplicationDbContext context, ILogger<DatabaseOptimizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OptimizeIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Starting database index optimization...");

            // For SQL Server, we can rebuild indexes and update statistics
            var tables = new[]
            {
                "Employees", "Departments", "WorkLocations", "WorkShifts", "WorkShiftLogs",
                "RequestLogs", "DuplicateDetectionLogs", "AuditLogs", "AspNetUsers"
            };

            foreach (var table in tables)
            {
                try
                {
                    // Rebuild indexes for better performance
                    await _context.Database.ExecuteSqlRawAsync($"ALTER INDEX ALL ON [{table}] REBUILD");
                    
                    // Update table statistics
                    await _context.Database.ExecuteSqlRawAsync($"UPDATE STATISTICS [{table}]");
                    
                    _logger.LogInformation("Optimized indexes and statistics for table {TableName}", table);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not optimize table {TableName}", table);
                }
            }

            _logger.LogInformation("Database index optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database optimization");
        }
    }

    public async Task AnalyzeTableStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Analyzing database table statistics...");

            // Get table sizes and row counts
            var query = @"
                SELECT 
                    t.NAME AS TableName,
                    s.Name AS SchemaName,
                    p.rows AS RowCounts,
                    SUM(a.total_pages) * 8 AS TotalSpaceKB,
                    SUM(a.used_pages) * 8 AS UsedSpaceKB,
                    (SUM(a.total_pages) - SUM(a.used_pages)) * 8 AS UnusedSpaceKB
                FROM 
                    sys.tables t
                INNER JOIN      
                    sys.indexes i ON t.OBJECT_ID = i.object_id
                INNER JOIN 
                    sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                INNER JOIN 
                    sys.allocation_units a ON p.partition_id = a.container_id
                LEFT OUTER JOIN 
                    sys.schemas s ON t.schema_id = s.schema_id
                WHERE 
                    t.NAME NOT LIKE 'dt%' 
                    AND t.is_ms_shipped = 0
                    AND i.OBJECT_ID > 255 
                GROUP BY 
                    t.Name, s.Name, p.Rows
                ORDER BY 
                    TotalSpaceKB DESC";

            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            
            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync();
            }

            using var result = await command.ExecuteReaderAsync();
            
            _logger.LogInformation("=== DATABASE TABLE STATISTICS ===");
            
            while (await result.ReadAsync())
            {
                var tableName = result["TableName"].ToString();
                var rowCount = Convert.ToInt64(result["RowCounts"]);
                var totalSpaceKB = Convert.ToInt64(result["TotalSpaceKB"]);
                var usedSpaceKB = Convert.ToInt64(result["UsedSpaceKB"]);
                
                _logger.LogInformation("?? {TableName}: {RowCount:N0} rows, {TotalSpaceMB:F1} MB total, {UsedSpaceMB:F1} MB used",
                    tableName, rowCount, totalSpaceKB / 1024.0, usedSpaceKB / 1024.0);
            }
            
            _logger.LogInformation("=====================================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing table statistics");
        }
    }

    public async Task CleanupOldDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting cleanup of old data...");

            // Clean up old request logs (older than 30 days)
            var requestLogCutoff = DateTime.UtcNow.AddDays(-30);
            var deletedRequestLogs = await _context.RequestLogs
                .Where(rl => rl.Timestamp < requestLogCutoff)
                .ExecuteDeleteAsync();
            
            if (deletedRequestLogs > 0)
            {
                _logger.LogInformation("Deleted {Count} old request logs", deletedRequestLogs);
            }

            // Clean up old duplicate detection logs (older than 90 days)
            var duplicateCutoff = DateTime.UtcNow.AddDays(-90);
            var deletedDuplicateLogs = await _context.DuplicateDetectionLogs
                .Where(ddl => ddl.DetectedAt < duplicateCutoff)
                .ExecuteDeleteAsync();
            
            if (deletedDuplicateLogs > 0)
            {
                _logger.LogInformation("Deleted {Count} old duplicate detection logs", deletedDuplicateLogs);
            }

            // Clean up old audit logs (older than 365 days, keep high-risk logs)
            var auditCutoff = DateTime.UtcNow.AddDays(-365);
            var deletedAuditLogs = await _context.AuditLogs
                .Where(al => al.Timestamp < auditCutoff && 
                           al.RiskLevel != "HIGH" && 
                           al.RiskLevel != "CRITICAL")
                .ExecuteDeleteAsync();
            
            if (deletedAuditLogs > 0)
            {
                _logger.LogInformation("Deleted {Count} old low-risk audit logs", deletedAuditLogs);
            }

            // Clean up old inactive work shifts (older than 2 years)
            var workShiftCutoff = DateTime.UtcNow.AddYears(-2);
            var deletedWorkShifts = await _context.WorkShifts
                .Where(ws => !ws.IsActive && ws.CreatedAt < workShiftCutoff)
                .ExecuteDeleteAsync();
            
            if (deletedWorkShifts > 0)
            {
                _logger.LogInformation("Deleted {Count} old inactive work shifts", deletedWorkShifts);
            }

            _logger.LogInformation("Old data cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during old data cleanup");
        }
    }

    public async Task CheckIndexUsageAsync()
    {
        try
        {
            _logger.LogInformation("Checking index usage statistics...");

            var query = @"
                SELECT 
                    OBJECT_NAME(i.OBJECT_ID) AS TableName,
                    i.name AS IndexName,
                    i.type_desc AS IndexType,
                    s.user_seeks,
                    s.user_scans,
                    s.user_lookups,
                    s.user_updates,
                    s.last_user_seek,
                    s.last_user_scan,
                    s.last_user_lookup
                FROM 
                    sys.indexes i
                LEFT JOIN 
                    sys.dm_db_index_usage_stats s ON i.OBJECT_ID = s.OBJECT_ID AND i.index_id = s.index_id
                WHERE 
                    OBJECTPROPERTY(i.OBJECT_ID,'IsUserTable') = 1
                    AND i.type_desc IN ('CLUSTERED', 'NONCLUSTERED')
                ORDER BY 
                    OBJECT_NAME(i.OBJECT_ID), i.name";

            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            
            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync();
            }

            using var result = await command.ExecuteReaderAsync();
            
            _logger.LogInformation("=== INDEX USAGE STATISTICS ===");
            
            while (await result.ReadAsync())
            {
                var tableName = result["TableName"].ToString();
                var indexName = result["IndexName"].ToString();
                var seeks = result["user_seeks"] != DBNull.Value ? Convert.ToInt64(result["user_seeks"]) : 0;
                var scans = result["user_scans"] != DBNull.Value ? Convert.ToInt64(result["user_scans"]) : 0;
                var lookups = result["user_lookups"] != DBNull.Value ? Convert.ToInt64(result["user_lookups"]) : 0;
                var updates = result["user_updates"] != DBNull.Value ? Convert.ToInt64(result["user_updates"]) : 0;

                var totalReads = seeks + scans + lookups;
                
                if (totalReads == 0 && updates > 0)
                {
                    _logger.LogWarning("?? Unused index: {TableName}.{IndexName} - {Updates} updates, 0 reads",
                        tableName, indexName, updates);
                }
                else if (totalReads > 0)
                {
                    _logger.LogInformation("?? {TableName}.{IndexName}: {Reads} reads, {Updates} updates",
                        tableName, indexName, totalReads, updates);
                }
            }
            
            _logger.LogInformation("===============================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking index usage");
        }
    }

    public async Task<DatabaseHealthReport> GetDatabaseHealthAsync()
    {
        var report = new DatabaseHealthReport
        {
            GeneratedAt = DateTime.UtcNow,
            DatabaseName = _context.Database.GetDbConnection().Database
        };

        try
        {
            // Table counts
            report.EmployeeCount = await _context.Employees.CountAsync(e => e.IsActive);
            report.DepartmentCount = await _context.Departments.CountAsync(d => d.IsActive);
            report.WorkShiftCount = await _context.WorkShifts.CountAsync(ws => ws.IsActive);
            report.RequestLogCount = await _context.RequestLogs.CountAsync();
            report.AuditLogCount = await _context.AuditLogs.CountAsync();
            
            // Recent activity counts (last 24 hours)
            var yesterday = DateTime.UtcNow.AddDays(-1);
            report.RecentRequestsCount = await _context.RequestLogs.CountAsync(rl => rl.Timestamp >= yesterday);
            report.RecentSpamCount = await _context.RequestLogs.CountAsync(rl => rl.Timestamp >= yesterday && rl.IsSpamDetected);
            report.RecentDuplicateAttempts = await _context.DuplicateDetectionLogs.CountAsync(ddl => ddl.DetectedAt >= yesterday);
            
            // Performance metrics
            var slowRequests = await _context.RequestLogs
                .Where(rl => rl.Timestamp >= yesterday && rl.ResponseTimeMs > 2000)
                .CountAsync();
            
            report.SlowRequestsCount = slowRequests;
            report.AverageResponseTime = await _context.RequestLogs
                .Where(rl => rl.Timestamp >= yesterday)
                .AverageAsync(rl => (double?)rl.ResponseTimeMs) ?? 0;

            // Health indicators
            report.IsHealthy = report.RecentSpamCount < 100 && 
                             report.RecentDuplicateAttempts < 50 && 
                             report.SlowRequestsCount < 100;
            
            report.HealthScore = CalculateHealthScore(report);
            
            _logger.LogInformation("Database health report generated: {HealthScore}/100 - {Status}",
                report.HealthScore, report.IsHealthy ? "HEALTHY" : "NEEDS ATTENTION");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating database health report");
            report.IsHealthy = false;
            report.HealthScore = 0;
        }

        return report;
    }

    private int CalculateHealthScore(DatabaseHealthReport report)
    {
        var score = 100;
        
        // Deduct points for spam activity
        if (report.RecentSpamCount > 50) score -= 20;
        else if (report.RecentSpamCount > 20) score -= 10;
        
        // Deduct points for duplicate attempts
        if (report.RecentDuplicateAttempts > 20) score -= 15;
        else if (report.RecentDuplicateAttempts > 10) score -= 5;
        
        // Deduct points for slow requests
        if (report.SlowRequestsCount > 50) score -= 15;
        else if (report.SlowRequestsCount > 20) score -= 5;
        
        // Deduct points for high average response time
        if (report.AverageResponseTime > 1000) score -= 10;
        else if (report.AverageResponseTime > 500) score -= 5;
        
        return Math.Max(0, score);
    }
}

public class DatabaseHealthReport
{
    public DateTime GeneratedAt { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public int HealthScore { get; set; } // 0-100
    
    // Table counts
    public int EmployeeCount { get; set; }
    public int DepartmentCount { get; set; }
    public int WorkShiftCount { get; set; }
    public long RequestLogCount { get; set; }
    public long AuditLogCount { get; set; }
    
    // Recent activity (last 24 hours)
    public int RecentRequestsCount { get; set; }
    public int RecentSpamCount { get; set; }
    public int RecentDuplicateAttempts { get; set; }
    public int SlowRequestsCount { get; set; }
    public double AverageResponseTime { get; set; }
    
    // Recommendations
    public List<string> Recommendations { get; set; } = new();
}