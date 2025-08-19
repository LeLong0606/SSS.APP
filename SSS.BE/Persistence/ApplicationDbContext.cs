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

    // ===== EXISTING ENTITIES =====
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<WorkLocation> WorkLocations { get; set; }
    public DbSet<WorkShift> WorkShifts { get; set; }
    public DbSet<WorkShiftLog> WorkShiftLogs { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<DuplicateDetectionLog> DuplicateDetectionLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    // ===== NEW ATTENDANCE MANAGEMENT ENTITIES =====
    public DbSet<ShiftTemplate> ShiftTemplates { get; set; }
    public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
    public DbSet<ShiftCalendar> ShiftCalendars { get; set; }
    public DbSet<AttendanceEvent> AttendanceEvents { get; set; }
    public DbSet<AttendanceDaily> AttendanceDaily { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<PayrollPeriod> PayrollPeriods { get; set; }
    public DbSet<PayrollSummary> PayrollSummaries { get; set; }

    // ===== NEW IMAGE MANAGEMENT ENTITIES =====
    public DbSet<ImageFile> ImageFiles { get; set; }
    public DbSet<EmployeePhoto> EmployeePhotos { get; set; }
    public DbSet<AttendancePhoto> AttendancePhotos { get; set; }
    public DbSet<LeaveRequestAttachment> LeaveRequestAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ===== EXISTING ENTITY CONFIGURATIONS =====
        ConfigureIdentityEntities(builder);
        ConfigureEmployeeEntities(builder);
        ConfigureDepartmentEntities(builder);
        ConfigureWorkShiftEntities(builder);
        ConfigureSecurityEntities(builder);

        // ===== NEW ATTENDANCE MANAGEMENT CONFIGURATIONS =====
        ConfigureAttendanceEntities(builder);

        // ===== NEW IMAGE MANAGEMENT CONFIGURATIONS =====
        ConfigureImageEntities(builder);

        // Keep default Identity table names
        ConfigureIdentityTableNames(builder);
    }

    private void ConfigureIdentityEntities(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode).IsUnique().HasDatabaseName("IX_ApplicationUser_EmployeeCode");
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_ApplicationUser_Email");
            entity.HasIndex(e => e.NormalizedEmail).IsUnique().HasDatabaseName("IX_ApplicationUser_NormalizedEmail");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApplicationUser_IsActive");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ApplicationUser_CreatedAt");
            
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
        });
    }

    private void ConfigureEmployeeEntities(ModelBuilder builder)
    {
        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            
            // Unique constraints
            entity.HasIndex(e => e.EmployeeCode).IsUnique().HasDatabaseName("IX_Employee_EmployeeCode");
            
            // Performance indexes
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Employee_IsActive");
            entity.HasIndex(e => e.DepartmentId).HasDatabaseName("IX_Employee_DepartmentId");
            entity.HasIndex(e => e.IsTeamLeader).HasDatabaseName("IX_Employee_IsTeamLeader");
            entity.HasIndex(e => new { e.DepartmentId, e.IsActive }).HasDatabaseName("IX_Employee_DepartmentId_IsActive");
            entity.HasIndex(e => new { e.IsActive, e.IsTeamLeader }).HasDatabaseName("IX_Employee_IsActive_IsTeamLeader");
            entity.HasIndex(e => e.FullName).HasDatabaseName("IX_Employee_FullName");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Employee_CreatedAt");
            
            // Relationships
            entity.HasOne(e => e.Department).WithMany(d => d.Employees).HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            
            // Precision and constraints
            entity.Property(e => e.Salary).HasPrecision(18, 2);
            entity.Property(e => e.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
        });
    }

    private void ConfigureDepartmentEntities(ModelBuilder builder)
    {
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            
            entity.HasIndex(d => d.DepartmentCode).IsUnique().HasDatabaseName("IX_Department_DepartmentCode").HasFilter("[DepartmentCode] IS NOT NULL");
            entity.HasIndex(d => d.Name).IsUnique().HasDatabaseName("IX_Department_Name");
            entity.HasIndex(d => d.IsActive).HasDatabaseName("IX_Department_IsActive");
            entity.HasIndex(d => d.TeamLeaderId).HasDatabaseName("IX_Department_TeamLeaderId").HasFilter("[TeamLeaderId] IS NOT NULL");
            
            entity.HasOne(d => d.TeamLeader).WithOne().HasForeignKey<Department>(d => d.TeamLeaderId).HasPrincipalKey<Employee>(e => e.EmployeeCode).OnDelete(DeleteBehavior.SetNull);
            
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.DepartmentCode).HasMaxLength(50);
            entity.Property(d => d.Description).HasMaxLength(1000);
        });
    }

    private void ConfigureWorkShiftEntities(ModelBuilder builder)
    {
        builder.Entity<WorkLocation>(entity =>
        {
            entity.ToTable("WorkLocations");
            
            entity.HasIndex(w => w.LocationCode).IsUnique().HasDatabaseName("IX_WorkLocation_LocationCode").HasFilter("[LocationCode] IS NOT NULL");
            entity.HasIndex(w => w.Name).IsUnique().HasDatabaseName("IX_WorkLocation_Name");
            entity.HasIndex(w => w.IsActive).HasDatabaseName("IX_WorkLocation_IsActive");
            
            entity.Property(w => w.Name).HasMaxLength(200).IsRequired();
            entity.Property(w => w.LocationCode).HasMaxLength(50);
            entity.Property(w => w.Address).HasMaxLength(500);
            entity.Property(w => w.Description).HasMaxLength(1000);
        });

        builder.Entity<WorkShift>(entity =>
        {
            entity.ToTable("WorkShifts");
            
            entity.HasIndex(w => new { w.EmployeeCode, w.ShiftDate, w.StartTime, w.EndTime }).IsUnique().HasDatabaseName("IX_WorkShift_Employee_DateTime_Unique").HasFilter("[IsActive] = 1");
            entity.HasIndex(w => new { w.EmployeeCode, w.ShiftDate }).HasDatabaseName("IX_WorkShift_EmployeeCode_ShiftDate");
            entity.HasIndex(w => new { w.ShiftDate, w.WorkLocationId }).HasDatabaseName("IX_WorkShift_ShiftDate_WorkLocationId");
            entity.HasIndex(w => new { w.ShiftDate, w.IsActive }).HasDatabaseName("IX_WorkShift_ShiftDate_IsActive");
            
            entity.HasOne(w => w.Employee).WithMany().HasForeignKey(w => w.EmployeeCode).HasPrincipalKey(e => e.EmployeeCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(w => w.WorkLocation).WithMany(wl => wl.WorkShifts).HasForeignKey(w => w.WorkLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(w => w.AssignedByEmployee).WithMany().HasForeignKey(w => w.AssignedByEmployeeCode).HasPrincipalKey(e => e.EmployeeCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(w => w.ModifiedByEmployee).WithMany().HasForeignKey(w => w.ModifiedByEmployeeCode).HasPrincipalKey(e => e.EmployeeCode).OnDelete(DeleteBehavior.SetNull);
            
            entity.Property(w => w.TotalHours).HasPrecision(5, 2);
            entity.Property(w => w.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(w => w.AssignedByEmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(w => w.ModifiedByEmployeeCode).HasMaxLength(50);
            entity.Property(w => w.ModificationReason).HasMaxLength(500);
        });

        builder.Entity<WorkShiftLog>(entity =>
        {
            entity.ToTable("WorkShiftLogs");
            
            entity.HasIndex(wsl => new { wsl.WorkShiftId, wsl.PerformedAt }).HasDatabaseName("IX_WorkShiftLog_WorkShiftId_PerformedAt");
            entity.HasIndex(wsl => new { wsl.PerformedByEmployeeCode, wsl.PerformedAt }).HasDatabaseName("IX_WorkShiftLog_PerformedBy_PerformedAt");
            entity.HasIndex(wsl => wsl.Action).HasDatabaseName("IX_WorkShiftLog_Action");
            
            entity.HasOne(wsl => wsl.WorkShift).WithMany().HasForeignKey(wsl => wsl.WorkShiftId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(wsl => wsl.PerformedByEmployee).WithMany().HasForeignKey(wsl => wsl.PerformedByEmployeeCode).HasPrincipalKey(e => e.EmployeeCode).OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(wsl => wsl.Action).HasMaxLength(50).IsRequired();
            entity.Property(wsl => wsl.PerformedByEmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(wsl => wsl.Reason).HasMaxLength(500);
            entity.Property(wsl => wsl.Comments).HasMaxLength(1000);
        });
    }

    private void ConfigureSecurityEntities(ModelBuilder builder)
    {
        builder.Entity<RequestLog>(entity =>
        {
            entity.ToTable("RequestLogs");
            
            entity.HasIndex(rl => new { rl.IpAddress, rl.Timestamp }).HasDatabaseName("IX_RequestLog_IpAddress_Timestamp");
            entity.HasIndex(rl => new { rl.UserId, rl.Timestamp }).HasDatabaseName("IX_RequestLog_UserId_Timestamp");
            entity.HasIndex(rl => new { rl.Endpoint, rl.Timestamp }).HasDatabaseName("IX_RequestLog_Endpoint_Timestamp");
            entity.HasIndex(rl => rl.RequestHash).HasDatabaseName("IX_RequestLog_RequestHash");
            entity.HasIndex(rl => rl.IsSpamDetected).HasDatabaseName("IX_RequestLog_IsSpamDetected");
            
            entity.Property(rl => rl.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(rl => rl.UserId).HasMaxLength(450);
            entity.Property(rl => rl.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(rl => rl.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(rl => rl.UserAgent).HasMaxLength(500);
        });

        builder.Entity<DuplicateDetectionLog>(entity =>
        {
            entity.ToTable("DuplicateDetectionLogs");
            
            entity.HasIndex(ddl => new { ddl.EntityType, ddl.EntityId }).HasDatabaseName("IX_DuplicateDetectionLog_EntityType_EntityId");
            entity.HasIndex(ddl => ddl.DataHash).HasDatabaseName("IX_DuplicateDetectionLog_DataHash");
            entity.HasIndex(ddl => ddl.DetectedAt).HasDatabaseName("IX_DuplicateDetectionLog_DetectedAt");
            
            entity.Property(ddl => ddl.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(ddl => ddl.EntityId).HasMaxLength(50).IsRequired();
            entity.Property(ddl => ddl.DataHash).HasMaxLength(64).IsRequired();
            entity.Property(ddl => ddl.UserId).HasMaxLength(450);
            entity.Property(ddl => ddl.Action).HasMaxLength(50).IsRequired();
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            
            entity.HasIndex(al => new { al.TableName, al.RecordId }).HasDatabaseName("IX_AuditLog_TableName_RecordId");
            entity.HasIndex(al => new { al.UserId, al.Timestamp }).HasDatabaseName("IX_AuditLog_UserId_Timestamp");
            entity.HasIndex(al => al.Action).HasDatabaseName("IX_AuditLog_Action");
            
            entity.Property(al => al.TableName).HasMaxLength(100).IsRequired();
            entity.Property(al => al.RecordId).HasMaxLength(50).IsRequired();
            entity.Property(al => al.Action).HasMaxLength(50).IsRequired();
            entity.Property(al => al.UserId).HasMaxLength(450);
            entity.Property(al => al.UserName).HasMaxLength(256);
            entity.Property(al => al.IpAddress).HasMaxLength(45);
        });
    }

    private void ConfigureAttendanceEntities(ModelBuilder builder)
    {
        // For now, use basic configurations (can be enhanced later)
        ConfigureBasicAttendanceEntities(builder);
    }

    private void ConfigureBasicAttendanceEntities(ModelBuilder builder)
    {
        // ===== ShiftTemplate Configuration =====
        builder.Entity<ShiftTemplate>(entity =>
        {
            entity.ToTable("ShiftTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Code).IsRequired().HasMaxLength(20);
            entity.Property(x => x.StandardHours).HasPrecision(5, 2);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        // ===== ShiftAssignment Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION PROPERTIES =====
        builder.Entity<ShiftAssignment>(entity =>
        {
            entity.ToTable("ShiftAssignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.RecurrencePattern).HasMaxLength(20);
            entity.Property(x => x.WeekDays).HasMaxLength(20);
            entity.Property(x => x.AssignedBy).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.AssignedByEmployee);
            entity.Ignore(x => x.ShiftCalendars);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(x => x.ShiftTemplate)
                .WithMany()
                .HasForeignKey(x => x.ShiftTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(x => x.WorkLocation)
                .WithMany()
                .HasForeignKey(x => x.WorkLocationId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(x => new { x.EmployeeCode, x.IsActive });
            entity.HasIndex(x => new { x.StartDate, x.EndDate });
        });

        // ===== ShiftCalendar Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<ShiftCalendar>(entity =>
        {
            entity.ToTable("ShiftCalendars");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.ShiftStatus).HasMaxLength(20);
            entity.Property(x => x.StandardHours).HasPrecision(5, 2);
            entity.Property(x => x.Notes).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.AttendanceEvents);
            entity.Ignore(x => x.AttendanceDaily);
            
            // ? ONLY configure explicitly allowed relationships  
            entity.HasOne(x => x.ShiftAssignment)
                .WithMany()
                .HasForeignKey(x => x.ShiftAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(x => x.ShiftTemplate)
                .WithMany()
                .HasForeignKey(x => x.ShiftTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(x => x.WorkLocation)
                .WithMany()
                .HasForeignKey(x => x.WorkLocationId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(x => new { x.EmployeeCode, x.ShiftDate }).IsUnique();
        });

        // ===== AttendanceEvent Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<AttendanceEvent>(entity =>
        {
            entity.ToTable("AttendanceEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(20);
            entity.Property(x => x.DeviceInfo).HasMaxLength(200);
            entity.Property(x => x.IPAddress).HasMaxLength(50);
            entity.Property(x => x.Latitude).HasPrecision(10, 6);
            entity.Property(x => x.Longitude).HasPrecision(10, 6);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(20);
            entity.Property(x => x.ApprovedBy).HasMaxLength(50);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.ApprovedByEmployee);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(x => x.WorkLocation)
                .WithMany()
                .HasForeignKey(x => x.WorkLocationId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(x => x.ShiftCalendar)
                .WithMany()
                .HasForeignKey(x => x.ShiftCalendarId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(x => new { x.EmployeeCode, x.EventDateTime });
        });

        // ===== AttendanceDaily Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<AttendanceDaily>(entity =>
        {
            entity.ToTable("AttendanceDaily");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.WorkedHours).HasPrecision(5, 2);
            entity.Property(x => x.OvertimeHours).HasPrecision(5, 2);
            entity.Property(x => x.WorkedMinutes).HasPrecision(8, 2);
            entity.Property(x => x.OvertimeMinutes).HasPrecision(8, 2);
            entity.Property(x => x.StandardHours).HasPrecision(5, 2);
            entity.Property(x => x.DeductedHours).HasPrecision(5, 2);
            entity.Property(x => x.ActualWorkDays).HasPrecision(3, 2);
            entity.Property(x => x.AttendanceStatus).HasMaxLength(20);
            entity.Property(x => x.ApprovedBy).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.ApprovedByEmployee);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(x => x.ShiftCalendar)
                .WithOne()
                .HasForeignKey<AttendanceDaily>(x => x.ShiftCalendarId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(x => new { x.EmployeeCode, x.AttendanceDate }).IsUnique();
        });

        // ===== LeaveRequest Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.LeaveType).IsRequired().HasMaxLength(50);
            entity.Property(x => x.TotalDays).HasPrecision(5, 1);
            entity.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(20);
            entity.Property(x => x.ApprovedBy).HasMaxLength(50);
            entity.Property(x => x.ApprovalNotes).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.ApprovedByEmployee);
            
            entity.HasIndex(x => x.EmployeeCode);
        });

        // ===== OvertimeRequest Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<OvertimeRequest>(entity =>
        {
            entity.ToTable("OvertimeRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.PlannedHours).HasPrecision(5, 2);
            entity.Property(x => x.ActualHours).HasPrecision(5, 2);
            entity.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.ApprovalStatus).HasMaxLength(20);
            entity.Property(x => x.ApprovedBy).HasMaxLength(50);
            entity.Property(x => x.ApprovalNotes).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            entity.Ignore(x => x.ApprovedByEmployee);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(x => x.WorkLocation)
                .WithMany()
                .HasForeignKey(x => x.WorkLocationId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(x => x.EmployeeCode);
        });

        // ===== Holiday Configuration =====
        builder.Entity<Holiday>(entity =>
        {
            entity.ToTable("Holidays");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.HolidayType).HasMaxLength(50);
            entity.Property(x => x.PayMultiplier).HasPrecision(5, 2);
            entity.Property(x => x.Description).HasMaxLength(500);
            
            entity.HasIndex(x => x.HolidayDate);
        });

        // ===== PayrollPeriod Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<PayrollPeriod>(entity =>
        {
            entity.ToTable("PayrollPeriods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PeriodName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.PeriodType).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.Property(x => x.LockedBy).HasMaxLength(50);
            entity.Property(x => x.FinalizedBy).HasMaxLength(50);
            entity.Property(x => x.ExportedBy).HasMaxLength(50);
            entity.Property(x => x.ExcelFilePath).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.LockedByEmployee);
            entity.Ignore(x => x.FinalizedByEmployee);
            entity.Ignore(x => x.ExportedByEmployee);
            entity.Ignore(x => x.PayrollSummaries);
            
            entity.HasIndex(x => new { x.StartDate, x.EndDate });
        });

        // ===== PayrollSummary Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<PayrollSummary>(entity =>
        {
            entity.ToTable("PayrollSummaries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.ActualWorkDays).HasPrecision(8, 2);
            entity.Property(x => x.TotalWorkHours).HasPrecision(8, 2);
            entity.Property(x => x.TotalOvertimeHours).HasPrecision(8, 2);
            entity.Property(x => x.AnnualLeaveDays).HasPrecision(5, 1);
            entity.Property(x => x.SickLeaveDays).HasPrecision(5, 1);
            entity.Property(x => x.UnpaidLeaveDays).HasPrecision(5, 1);
            entity.Property(x => x.HolidayWorkDays).HasPrecision(5, 2);
            entity.Property(x => x.AbsentDays).HasPrecision(5, 2);
            entity.Property(x => x.DeductedHours).HasPrecision(8, 2);
            entity.Property(x => x.CalculationNotes).HasMaxLength(1000);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(x => x.Employee);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(x => x.PayrollPeriod)
                .WithMany()
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeCode }).IsUnique();
        });
    }

    private void ConfigureImageEntities(ModelBuilder builder)
    {
        // ===== ImageFile Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<ImageFile>(entity =>
        {
            entity.ToTable("ImageFiles");
            
            entity.HasIndex(img => img.FileHash).IsUnique().HasDatabaseName("IX_ImageFile_FileHash");
            entity.HasIndex(img => new { img.UploadedBy, img.UploadedAt }).HasDatabaseName("IX_ImageFile_UploadedBy_UploadedAt");
            entity.HasIndex(img => img.IsActive).HasDatabaseName("IX_ImageFile_IsActive");
            entity.HasIndex(img => img.FileType).HasDatabaseName("IX_ImageFile_FileType");
            
            entity.Property(img => img.FileName).HasMaxLength(255).IsRequired();
            entity.Property(img => img.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(img => img.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(img => img.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(img => img.FileHash).HasMaxLength(64).IsRequired();
            entity.Property(img => img.UploadedBy).HasMaxLength(50).IsRequired();
            entity.Property(img => img.FileType).HasMaxLength(50).IsRequired();
            entity.Property(img => img.DeletedBy).HasMaxLength(50);
            entity.Property(img => img.DeletedReason).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(img => img.UploadedByEmployee);
        });

        // ===== EmployeePhoto Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<EmployeePhoto>(entity =>
        {
            entity.ToTable("EmployeePhotos");
            
            entity.HasIndex(ep => ep.EmployeeCode).IsUnique().HasDatabaseName("IX_EmployeePhoto_EmployeeCode");
            entity.HasIndex(ep => ep.ImageFileId).HasDatabaseName("IX_EmployeePhoto_ImageFileId");
            entity.HasIndex(ep => ep.IsActive).HasDatabaseName("IX_EmployeePhoto_IsActive");
            
            entity.Property(ep => ep.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(ep => ep.SetBy).HasMaxLength(50).IsRequired();
            entity.Property(ep => ep.Notes).HasMaxLength(500);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(ep => ep.Employee);
            entity.Ignore(ep => ep.SetByEmployee);
            
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(ep => ep.ImageFile)
                .WithMany()
                .HasForeignKey(ep => ep.ImageFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== AttendancePhoto Configuration - EXPLICITLY IGNORE EMPLOYEE NAVIGATION =====
        builder.Entity<AttendancePhoto>(entity =>
        {
            entity.ToTable("AttendancePhotos");
            
            entity.HasIndex(ap => ap.AttendanceEventId).HasDatabaseName("IX_AttendancePhoto_AttendanceEventId");
            entity.HasIndex(ap => new { ap.EmployeeCode, ap.TakenAt }).HasDatabaseName("IX_AttendancePhoto_Employee_TakenAt");
            entity.HasIndex(ap => ap.PhotoType).HasDatabaseName("IX_AttendancePhoto_PhotoType");
            
            entity.Property(ap => ap.EmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(ap => ap.PhotoType).HasMaxLength(20).IsRequired();
            entity.Property(ap => ap.Location).HasMaxLength(200);
            entity.Property(ap => ap.DeviceInfo).HasMaxLength(200);
            entity.Property(ap => ap.Notes).HasMaxLength(500);
            entity.Property(ap => ap.Latitude).HasPrecision(10, 6);
            entity.Property(ap => ap.Longitude).HasPrecision(10, 6);
            entity.Property(ap => ap.VerifiedBy).HasMaxLength(50);
            entity.Property(ap => ap.FaceConfidenceScore).HasPrecision(5, 4);
            
            // ? FIX: EXPLICITLY IGNORE navigation properties to prevent automatic FK creation
            entity.Ignore(ap => ap.Employee);
                
            // ? ONLY configure explicitly allowed relationships
            entity.HasOne(ap => ap.ImageFile)
                .WithMany()
                .HasForeignKey(ap => ap.ImageFileId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(ap => ap.AttendanceEvent)
                .WithMany()
                .HasForeignKey(ap => ap.AttendanceEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== LeaveRequestAttachment Configuration =====
        builder.Entity<LeaveRequestAttachment>(entity =>
        {
            entity.ToTable("LeaveRequestAttachments");
            
            entity.HasIndex(lra => lra.LeaveRequestId).HasDatabaseName("IX_LeaveRequestAttachment_LeaveRequestId");
            entity.HasIndex(lra => lra.ImageFileId).HasDatabaseName("IX_LeaveRequestAttachment_ImageFileId");
            entity.HasIndex(lra => lra.AttachmentType).HasDatabaseName("IX_LeaveRequestAttachment_AttachmentType");
            
            entity.Property(lra => lra.AttachmentType).HasMaxLength(50).IsRequired();
            entity.Property(lra => lra.Description).HasMaxLength(500);
            entity.Property(lra => lra.ApprovedBy).HasMaxLength(50);
            entity.Property(lra => lra.ApprovalNotes).HasMaxLength(500);
            
            // ? FIX: Configure only allowed relationships
            entity.HasOne(lra => lra.LeaveRequest)
                .WithMany()
                .HasForeignKey(lra => lra.LeaveRequestId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(lra => lra.ImageFile)
                .WithMany()
                .HasForeignKey(lra => lra.ImageFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("AspNetRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("AspNetUserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("AspNetUserTokens");
    }
}