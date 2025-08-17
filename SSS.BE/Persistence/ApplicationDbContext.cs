using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Domain.Entities;

namespace SSS.BE.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Existing DbSets
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    
    // New DbSets for Work Shift Management
    public DbSet<WorkLocation> WorkLocations { get; set; }
    public DbSet<WorkShift> WorkShifts { get; set; }
    public DbSet<WorkShiftLog> WorkShiftLogs { get; set; }
    
    // New DbSets for Anti-Spam and Duplicate Prevention
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<DuplicateDetectionLog> DuplicateDetectionLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser with Enhanced Security
        builder.Entity<ApplicationUser>(entity =>
        {
            // Unique indexes for security
            entity.HasIndex(e => e.EmployeeCode)
                  .IsUnique()
                  .HasDatabaseName("IX_ApplicationUser_EmployeeCode");
            
            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_ApplicationUser_Email");
            
            entity.HasIndex(e => e.NormalizedEmail)
                  .IsUnique()
                  .HasDatabaseName("IX_ApplicationUser_NormalizedEmail");
            
            // Performance indexes
            entity.HasIndex(e => e.IsActive)
                  .HasDatabaseName("IX_ApplicationUser_IsActive");
            
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_ApplicationUser_CreatedAt");
            
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
        });

        // Configure Employee entity with Enhanced Indexing
        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            
            // ===== UNIQUE CONSTRAINTS (Prevent Duplicates) =====
            entity.HasIndex(e => e.EmployeeCode)
                  .IsUnique()
                  .HasDatabaseName("IX_Employee_EmployeeCode");

            // ===== PERFORMANCE INDEXES =====
            // Most common queries
            entity.HasIndex(e => e.IsActive)
                  .HasDatabaseName("IX_Employee_IsActive");
            
            entity.HasIndex(e => e.DepartmentId)
                  .HasDatabaseName("IX_Employee_DepartmentId");
            
            entity.HasIndex(e => e.IsTeamLeader)
                  .HasDatabaseName("IX_Employee_IsTeamLeader");
            
            // Composite indexes for common filter combinations
            entity.HasIndex(e => new { e.DepartmentId, e.IsActive })
                  .HasDatabaseName("IX_Employee_DepartmentId_IsActive");
            
            entity.HasIndex(e => new { e.IsActive, e.IsTeamLeader })
                  .HasDatabaseName("IX_Employee_IsActive_IsTeamLeader");
            
            entity.HasIndex(e => new { e.DepartmentId, e.IsTeamLeader, e.IsActive })
                  .HasDatabaseName("IX_Employee_DepartmentId_IsTeamLeader_IsActive");
            
            // Search optimization indexes
            entity.HasIndex(e => e.FullName)
                  .HasDatabaseName("IX_Employee_FullName");
            
            entity.HasIndex(e => e.Position)
                  .HasDatabaseName("IX_Employee_Position")
                  .HasFilter("[Position] IS NOT NULL");
            
            // Audit and tracking indexes
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_Employee_CreatedAt");
            
            entity.HasIndex(e => e.UpdatedAt)
                  .HasDatabaseName("IX_Employee_UpdatedAt")
                  .HasFilter("[UpdatedAt] IS NOT NULL");
            
            entity.HasIndex(e => e.HireDate)
                  .HasDatabaseName("IX_Employee_HireDate")
                  .HasFilter("[HireDate] IS NOT NULL");

            // Configure relationship with Department
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision for Salary
            entity.Property(e => e.Salary)
                  .HasPrecision(18, 2);
                  
            // Add data validation constraints
            entity.Property(e => e.EmployeeCode)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(e => e.FullName)
                  .HasMaxLength(200)
                  .IsRequired();
                  
            entity.Property(e => e.Position)
                  .HasMaxLength(100);
                  
            entity.Property(e => e.PhoneNumber)
                  .HasMaxLength(20);
                  
            entity.Property(e => e.Address)
                  .HasMaxLength(500);
        });

        // Configure Department entity with Enhanced Security
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            
            // ===== UNIQUE CONSTRAINTS =====
            entity.HasIndex(d => d.DepartmentCode)
                  .IsUnique()
                  .HasDatabaseName("IX_Department_DepartmentCode")
                  .HasFilter("[DepartmentCode] IS NOT NULL");
            
            entity.HasIndex(d => d.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_Department_Name");
            
            // ===== PERFORMANCE INDEXES =====
            entity.HasIndex(d => d.IsActive)
                  .HasDatabaseName("IX_Department_IsActive");
            
            entity.HasIndex(d => d.TeamLeaderId)
                  .HasDatabaseName("IX_Department_TeamLeaderId")
                  .HasFilter("[TeamLeaderId] IS NOT NULL");
            
            entity.HasIndex(d => new { d.IsActive, d.TeamLeaderId })
                  .HasDatabaseName("IX_Department_IsActive_TeamLeaderId");
            
            entity.HasIndex(d => d.CreatedAt)
                  .HasDatabaseName("IX_Department_CreatedAt");

            // Configure Team Leader relationship using EmployeeCode as principal key
            entity.HasOne(d => d.TeamLeader)
                  .WithOne()
                  .HasForeignKey<Department>(d => d.TeamLeaderId)
                  .HasPrincipalKey<Employee>(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            // Data validation constraints
            entity.Property(d => d.Name)
                  .HasMaxLength(200)
                  .IsRequired();
                  
            entity.Property(d => d.DepartmentCode)
                  .HasMaxLength(50);
                  
            entity.Property(d => d.Description)
                  .HasMaxLength(1000);
        });

        // Configure WorkLocation entity with Enhanced Indexing
        builder.Entity<WorkLocation>(entity =>
        {
            entity.ToTable("WorkLocations");
            
            // ===== UNIQUE CONSTRAINTS =====
            entity.HasIndex(w => w.LocationCode)
                  .IsUnique()
                  .HasDatabaseName("IX_WorkLocation_LocationCode")
                  .HasFilter("[LocationCode] IS NOT NULL");
            
            entity.HasIndex(w => w.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_WorkLocation_Name");
            
            // ===== PERFORMANCE INDEXES =====
            entity.HasIndex(w => w.IsActive)
                  .HasDatabaseName("IX_WorkLocation_IsActive");
            
            entity.HasIndex(w => w.CreatedAt)
                  .HasDatabaseName("IX_WorkLocation_CreatedAt");
                  
            // Data validation
            entity.Property(w => w.Name)
                  .HasMaxLength(200)
                  .IsRequired();
                  
            entity.Property(w => w.LocationCode)
                  .HasMaxLength(50);
                  
            entity.Property(w => w.Address)
                  .HasMaxLength(500);
                  
            entity.Property(w => w.Description)
                  .HasMaxLength(1000);
        });

        // Configure WorkShift entity with Advanced Anti-Duplicate Logic
        builder.Entity<WorkShift>(entity =>
        {
            entity.ToTable("WorkShifts");
            
            // ===== DUPLICATE PREVENTION CONSTRAINTS =====
            // Prevent duplicate shifts for same employee on same day/time
            entity.HasIndex(w => new { w.EmployeeCode, w.ShiftDate, w.StartTime, w.EndTime })
                  .IsUnique()
                  .HasDatabaseName("IX_WorkShift_Employee_DateTime_Unique")
                  .HasFilter("[IsActive] = 1"); // Only active shifts
            
            // ===== PERFORMANCE INDEXES =====
            // Most common query patterns
            entity.HasIndex(w => new { w.EmployeeCode, w.ShiftDate })
                  .HasDatabaseName("IX_WorkShift_EmployeeCode_ShiftDate");

            entity.HasIndex(w => new { w.ShiftDate, w.WorkLocationId })
                  .HasDatabaseName("IX_WorkShift_ShiftDate_WorkLocationId");
            
            entity.HasIndex(w => new { w.ShiftDate, w.IsActive })
                  .HasDatabaseName("IX_WorkShift_ShiftDate_IsActive");
            
            entity.HasIndex(w => new { w.EmployeeCode, w.IsActive })
                  .HasDatabaseName("IX_WorkShift_EmployeeCode_IsActive");
            
            entity.HasIndex(w => new { w.WorkLocationId, w.ShiftDate, w.IsActive })
                  .HasDatabaseName("IX_WorkShift_WorkLocationId_ShiftDate_IsActive");
            
            // Audit and management indexes
            entity.HasIndex(w => w.AssignedByEmployeeCode)
                  .HasDatabaseName("IX_WorkShift_AssignedByEmployeeCode");
            
            entity.HasIndex(w => w.ModifiedByEmployeeCode)
                  .HasDatabaseName("IX_WorkShift_ModifiedByEmployeeCode")
                  .HasFilter("[ModifiedByEmployeeCode] IS NOT NULL");
            
            entity.HasIndex(w => w.CreatedAt)
                  .HasDatabaseName("IX_WorkShift_CreatedAt");
            
            entity.HasIndex(w => w.IsModified)
                  .HasDatabaseName("IX_WorkShift_IsModified");
            
            // Time-based reporting indexes
            entity.HasIndex(w => new { w.ShiftDate, w.TotalHours })
                  .HasDatabaseName("IX_WorkShift_ShiftDate_TotalHours")
                  .HasFilter("[IsActive] = 1");

            // Configure relationships
            entity.HasOne(w => w.Employee)
                  .WithMany()
                  .HasForeignKey(w => w.EmployeeCode)
                  .HasPrincipalKey(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.WorkLocation)
                  .WithMany(wl => wl.WorkShifts)
                  .HasForeignKey(w => w.WorkLocationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.AssignedByEmployee)
                  .WithMany()
                  .HasForeignKey(w => w.AssignedByEmployeeCode)
                  .HasPrincipalKey(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.ModifiedByEmployee)
                  .WithMany()
                  .HasForeignKey(w => w.ModifiedByEmployeeCode)
                  .HasPrincipalKey(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision for TotalHours
            entity.Property(w => w.TotalHours)
                  .HasPrecision(5, 2);
                  
            // Data validation
            entity.Property(w => w.EmployeeCode)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(w => w.AssignedByEmployeeCode)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(w => w.ModifiedByEmployeeCode)
                  .HasMaxLength(50);
                  
            entity.Property(w => w.ModificationReason)
                  .HasMaxLength(500);
        });

        // Configure WorkShiftLog entity with Performance Optimization
        builder.Entity<WorkShiftLog>(entity =>
        {
            entity.ToTable("WorkShiftLogs");
            
            // ===== PERFORMANCE INDEXES =====
            // Audit trail queries
            entity.HasIndex(wsl => new { wsl.WorkShiftId, wsl.PerformedAt })
                  .HasDatabaseName("IX_WorkShiftLog_WorkShiftId_PerformedAt");
            
            entity.HasIndex(wsl => new { wsl.PerformedByEmployeeCode, wsl.PerformedAt })
                  .HasDatabaseName("IX_WorkShiftLog_PerformedBy_PerformedAt");
            
            entity.HasIndex(wsl => wsl.Action)
                  .HasDatabaseName("IX_WorkShiftLog_Action");
            
            entity.HasIndex(wsl => wsl.PerformedAt)
                  .HasDatabaseName("IX_WorkShiftLog_PerformedAt");

            // Configure relationships
            entity.HasOne(wsl => wsl.WorkShift)
                  .WithMany()
                  .HasForeignKey(wsl => wsl.WorkShiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wsl => wsl.PerformedByEmployee)
                  .WithMany()
                  .HasForeignKey(wsl => wsl.PerformedByEmployeeCode)
                  .HasPrincipalKey(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            // Data validation
            entity.Property(wsl => wsl.Action)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(wsl => wsl.PerformedByEmployeeCode)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(wsl => wsl.Reason)
                  .HasMaxLength(500);
                  
            entity.Property(wsl => wsl.Comments)
                  .HasMaxLength(1000);
        });
        
        // ===== NEW ENTITIES FOR SPAM AND DUPLICATE PREVENTION =====
        
        // Configure RequestLog entity for Spam Prevention
        builder.Entity<RequestLog>(entity =>
        {
            entity.ToTable("RequestLogs");
            
            // ===== PERFORMANCE INDEXES FOR SPAM DETECTION =====
            entity.HasIndex(rl => new { rl.IpAddress, rl.Timestamp })
                  .HasDatabaseName("IX_RequestLog_IpAddress_Timestamp");
            
            entity.HasIndex(rl => new { rl.UserId, rl.Timestamp })
                  .HasDatabaseName("IX_RequestLog_UserId_Timestamp");
            
            entity.HasIndex(rl => new { rl.Endpoint, rl.Timestamp })
                  .HasDatabaseName("IX_RequestLog_Endpoint_Timestamp");
            
            entity.HasIndex(rl => rl.RequestHash)
                  .HasDatabaseName("IX_RequestLog_RequestHash");
            
            entity.HasIndex(rl => rl.IsSpamDetected)
                  .HasDatabaseName("IX_RequestLog_IsSpamDetected");
            
            entity.HasIndex(rl => rl.Timestamp)
                  .HasDatabaseName("IX_RequestLog_Timestamp");
                  
            // Data validation
            entity.Property(rl => rl.IpAddress)
                  .HasMaxLength(45) // IPv6 support
                  .IsRequired();
                  
            entity.Property(rl => rl.UserId)
                  .HasMaxLength(450);
                  
            entity.Property(rl => rl.Endpoint)
                  .HasMaxLength(200)
                  .IsRequired();
                  
            entity.Property(rl => rl.RequestHash)
                  .HasMaxLength(64) // SHA-256 hash
                  .IsRequired();
                  
            entity.Property(rl => rl.UserAgent)
                  .HasMaxLength(500);
        });
        
        // Configure DuplicateDetectionLog entity
        builder.Entity<DuplicateDetectionLog>(entity =>
        {
            entity.ToTable("DuplicateDetectionLogs");
            
            // ===== INDEXES FOR DUPLICATE TRACKING =====
            entity.HasIndex(ddl => new { ddl.EntityType, ddl.EntityId })
                  .HasDatabaseName("IX_DuplicateDetectionLog_EntityType_EntityId");
            
            entity.HasIndex(ddl => ddl.DataHash)
                  .HasDatabaseName("IX_DuplicateDetectionLog_DataHash");
            
            entity.HasIndex(ddl => ddl.DetectedAt)
                  .HasDatabaseName("IX_DuplicateDetectionLog_DetectedAt");
            
            entity.HasIndex(ddl => new { ddl.UserId, ddl.DetectedAt })
                  .HasDatabaseName("IX_DuplicateDetectionLog_UserId_DetectedAt");
                  
            // Data validation
            entity.Property(ddl => ddl.EntityType)
                  .HasMaxLength(100)
                  .IsRequired();
                  
            entity.Property(ddl => ddl.EntityId)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(ddl => ddl.DataHash)
                  .HasMaxLength(64)
                  .IsRequired();
                  
            entity.Property(ddl => ddl.UserId)
                  .HasMaxLength(450);
                  
            entity.Property(ddl => ddl.Action)
                  .HasMaxLength(50)
                  .IsRequired();
        });
        
        // Configure AuditLog entity for Security Tracking
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            
            // ===== AUDIT INDEXES =====
            entity.HasIndex(al => new { al.TableName, al.RecordId })
                  .HasDatabaseName("IX_AuditLog_TableName_RecordId");
            
            entity.HasIndex(al => new { al.UserId, al.Timestamp })
                  .HasDatabaseName("IX_AuditLog_UserId_Timestamp");
            
            entity.HasIndex(al => al.Action)
                  .HasDatabaseName("IX_AuditLog_Action");
            
            entity.HasIndex(al => al.Timestamp)
                  .HasDatabaseName("IX_AuditLog_Timestamp");
            
            entity.HasIndex(al => al.IpAddress)
                  .HasDatabaseName("IX_AuditLog_IpAddress");
                  
            // Data validation
            entity.Property(al => al.TableName)
                  .HasMaxLength(100)
                  .IsRequired();
                  
            entity.Property(al => al.RecordId)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(al => al.Action)
                  .HasMaxLength(50)
                  .IsRequired();
                  
            entity.Property(al => al.UserId)
                  .HasMaxLength(450);
                  
            entity.Property(al => al.UserName)
                  .HasMaxLength(256);
                  
            entity.Property(al => al.IpAddress)
                  .HasMaxLength(45);
        });

        // Keep default Identity table names
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("AspNetRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("AspNetUserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("AspNetUserTokens");
    }
}