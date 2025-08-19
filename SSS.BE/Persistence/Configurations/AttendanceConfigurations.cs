using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SSS.BE.Domain.Entities;

namespace SSS.BE.Persistence.Configurations;

/// <summary>
/// Database configuration cho các b?ng attendance management m?i
/// </summary>

public class ShiftTemplateConfiguration : IEntityTypeConfiguration<ShiftTemplate>
{
    public void Configure(EntityTypeBuilder<ShiftTemplate> builder)
    {
        builder.ToTable("ShiftTemplates");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.StandardHours).HasPrecision(5, 2);
        builder.Property(x => x.Description).HasMaxLength(500);
        
        // Index for performance
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Name });
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.RecurrencePattern).HasMaxLength(20);
        builder.Property(x => x.WeekDays).HasMaxLength(20);
        builder.Property(x => x.AssignedBy).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ShiftTemplate)
            .WithMany(x => x.ShiftAssignments)
            .HasForeignKey(x => x.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.WorkLocation)
            .WithMany()
            .HasForeignKey(x => x.WorkLocationId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(x => x.AssignedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(x => new { x.EmployeeCode, x.IsActive });
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
    }
}

public class ShiftCalendarConfiguration : IEntityTypeConfiguration<ShiftCalendar>
{
    public void Configure(EntityTypeBuilder<ShiftCalendar> builder)
    {
        builder.ToTable("ShiftCalendars");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ShiftStatus).HasMaxLength(20);
        builder.Property(x => x.StandardHours).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ShiftAssignment)
            .WithMany(x => x.ShiftCalendars)
            .HasForeignKey(x => x.ShiftAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ShiftTemplate)
            .WithMany()
            .HasForeignKey(x => x.ShiftTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Critical Index cho performance
        builder.HasIndex(x => new { x.EmployeeCode, x.ShiftDate }).IsUnique();
        builder.HasIndex(x => x.ShiftDate);
        builder.HasIndex(x => new { x.ShiftDate, x.IsActive });
    }
}

public class AttendanceEventConfiguration : IEntityTypeConfiguration<AttendanceEvent>
{
    public void Configure(EntityTypeBuilder<AttendanceEvent> builder)
    {
        builder.ToTable("AttendanceEvents");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.DeviceInfo).HasMaxLength(200);
        builder.Property(x => x.IPAddress).HasMaxLength(50);
        builder.Property(x => x.Latitude).HasPrecision(10, 6);
        builder.Property(x => x.Longitude).HasPrecision(10, 6);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.ApprovedBy).HasMaxLength(50);
        builder.Property(x => x.ApprovalStatus).HasMaxLength(20);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ShiftCalendar)
            .WithMany(x => x.AttendanceEvents)
            .HasForeignKey(x => x.ShiftCalendarId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Performance indexes - QUAN TR?NG cho b?ng này vì có th? có hàng tri?u records
        builder.HasIndex(x => new { x.EmployeeCode, x.EventDateTime });
        builder.HasIndex(x => x.EventDateTime);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.ApprovalStatus);
    }
}

public class AttendanceDailyConfiguration : IEntityTypeConfiguration<AttendanceDaily>
{
    public void Configure(EntityTypeBuilder<AttendanceDaily> builder)
    {
        builder.ToTable("AttendanceDaily");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.WorkedMinutes).HasPrecision(8, 2);
        builder.Property(x => x.WorkedHours).HasPrecision(5, 2);
        builder.Property(x => x.StandardHours).HasPrecision(5, 2);
        builder.Property(x => x.OvertimeMinutes).HasPrecision(8, 2);
        builder.Property(x => x.OvertimeHours).HasPrecision(5, 2);
        builder.Property(x => x.DeductedHours).HasPrecision(5, 2);
        builder.Property(x => x.ActualWorkDays).HasPrecision(3, 2);
        builder.Property(x => x.AttendanceStatus).HasMaxLength(20);
        builder.Property(x => x.ApprovedBy).HasMaxLength(50);
        builder.Property(x => x.ApprovalStatus).HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.SystemCalculationLog).HasMaxLength(500);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ShiftCalendar)
            .WithOne(x => x.AttendanceDaily)
            .HasForeignKey<AttendanceDaily>(x => x.ShiftCalendarId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Critical indexes
        builder.HasIndex(x => new { x.EmployeeCode, x.AttendanceDate }).IsUnique();
        builder.HasIndex(x => x.AttendanceDate);
        builder.HasIndex(x => x.AttendanceStatus);
        builder.HasIndex(x => x.ApprovalStatus);
        builder.HasIndex(x => x.HasTimeViolation);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TotalDays).HasPrecision(5, 2);
        builder.Property(x => x.LeaveType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.ApprovedBy).HasMaxLength(50);
        builder.Property(x => x.ApprovalStatus).HasMaxLength(20);
        builder.Property(x => x.ApprovalNotes).HasMaxLength(500);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(x => new { x.EmployeeCode, x.ApprovalStatus });
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
        builder.HasIndex(x => x.LeaveType);
    }
}

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable("OvertimeRequests");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PlannedHours).HasPrecision(5, 2);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.ApprovedBy).HasMaxLength(50);
        builder.Property(x => x.ApprovalStatus).HasMaxLength(20);
        builder.Property(x => x.ApprovalNotes).HasMaxLength(500);
        builder.Property(x => x.ActualHours).HasPrecision(5, 2);
        
        // Foreign Keys
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.WorkLocation)
            .WithMany()
            .HasForeignKey(x => x.WorkLocationId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(x => x.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(x => new { x.EmployeeCode, x.OvertimeDate });
        builder.HasIndex(x => x.ApprovalStatus);
        builder.HasIndex(x => x.OvertimeDate);
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PayMultiplier).HasPrecision(4, 2);
        builder.Property(x => x.HolidayType).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        
        // Indexes
        builder.HasIndex(x => x.HolidayDate);
        builder.HasIndex(x => new { x.HolidayDate, x.IsActive });
        builder.HasIndex(x => x.HolidayType);
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.PeriodName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PeriodType).HasMaxLength(20);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.LockedBy).HasMaxLength(50);
        builder.Property(x => x.FinalizedBy).HasMaxLength(50);
        builder.Property(x => x.ExcelFilePath).HasMaxLength(500);
        builder.Property(x => x.ExportedBy).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        
        // Foreign Keys
        builder.HasOne(x => x.LockedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.LockedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(x => x.FinalizedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.FinalizedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(x => x.ExportedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ExportedBy)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PeriodType);
    }
}

public class PayrollSummaryConfiguration : IEntityTypeConfiguration<PayrollSummary>
{
    public void Configure(EntityTypeBuilder<PayrollSummary> builder)
    {
        builder.ToTable("PayrollSummaries");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ActualWorkDays).HasPrecision(6, 2);
        builder.Property(x => x.TotalWorkHours).HasPrecision(8, 2);
        builder.Property(x => x.TotalOvertimeHours).HasPrecision(8, 2);
        builder.Property(x => x.AnnualLeaveDays).HasPrecision(5, 2);
        builder.Property(x => x.SickLeaveDays).HasPrecision(5, 2);
        builder.Property(x => x.UnpaidLeaveDays).HasPrecision(5, 2);
        builder.Property(x => x.AbsentDays).HasPrecision(5, 2);
        builder.Property(x => x.HolidayWorkDays).HasPrecision(5, 2);
        builder.Property(x => x.DeductedHours).HasPrecision(8, 2);
        builder.Property(x => x.CalculationNotes).HasMaxLength(1000);
        
        // Foreign Keys
        builder.HasOne(x => x.PayrollPeriod)
            .WithMany(x => x.PayrollSummaries)
            .HasForeignKey(x => x.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeCode)
            .HasPrincipalKey(x => x.EmployeeCode)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Critical indexes
        builder.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeCode }).IsUnique();
        builder.HasIndex(x => x.PayrollPeriodId);
        builder.HasIndex(x => x.EmployeeCode);
    }
}