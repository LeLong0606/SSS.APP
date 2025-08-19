using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Domain.Entities;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Persistence;

namespace SSS.BE.Infrastructure.Data;

/// <summary>
/// Comprehensive Data Seeder for the entire SSS.BE system
/// Code-First approach with auto seeding
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("?? Starting comprehensive data seeding...");

            // 1. Seed Identity Roles
            await SeedRolesAsync(roleManager, logger);

            // 2. Seed Work Locations
            await SeedWorkLocationsAsync(context, logger);

            // 3. Seed Departments
            await SeedDepartmentsAsync(context, logger);

            // 4. Seed Employees
            await SeedEmployeesAsync(context, logger);

            // 5. Seed Users & Identity
            await SeedUsersAsync(context, userManager, logger);

            // 6. Seed Shift Templates
            await SeedShiftTemplatesAsync(context, logger);

            // 7. Seed Holidays
            await SeedHolidaysAsync(context, logger);

            // 8. Seed Sample Shift Assignments
            await SeedShiftAssignmentsAsync(context, logger);

            // 9. Seed Sample Attendance Data
            await SeedSampleAttendanceDataAsync(context, logger);

            // 10. Seed Payroll Periods
            await SeedPayrollPeriodsAsync(context, logger);

            await context.SaveChangesAsync();
            
            logger.LogInformation("? Data seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "? Error during data seeding");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            "Administrator",
            "Director", 
            "TeamLeader",
            "Employee"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("? Created role: {Role}", role);
            }
        }
    }

    private static async Task SeedWorkLocationsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.WorkLocations.AnyAsync())
        {
            var locations = new[]
            {
                new WorkLocation
                {
                    Name = "Main Office - New York",
                    LocationCode = "NY_MAIN",
                    Address = "10th Floor, ABC Building, 123 Broadway, New York, NY",
                    Description = "Main office located in New York",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Branch Office - Los Angeles",
                    LocationCode = "LA_BRANCH",
                    Address = "15th Floor, XYZ Building, 456 Sunset Blvd, Los Angeles, CA",
                    Description = "Branch office located in Los Angeles",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Remote Work",
                    LocationCode = "REMOTE",
                    Address = "Work from Home",
                    Description = "Remote work location (Work from Home)",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.WorkLocations.AddRange(locations);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} work locations", locations.Length);
        }
    }

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Departments.AnyAsync())
        {
            var departments = new[]
            {
                new Department
                {
                    Name = "Information Technology Department",
                    DepartmentCode = "IT",
                    Description = "Responsible for IT system development and operations",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "Human Resources Department",
                    DepartmentCode = "HR",
                    Description = "Human resources management, recruitment and training",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "Accounting Department",
                    DepartmentCode = "ACCOUNTING",
                    Description = "Financial management, accounting and reporting",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} departments", departments.Length);
        }
    }

    private static async Task SeedEmployeesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Employees.AnyAsync())
        {
            var itDept = await context.Departments.FirstAsync(d => d.DepartmentCode == "IT");
            var hrDept = await context.Departments.FirstAsync(d => d.DepartmentCode == "HR");

            var employees = new[]
            {
                new Employee
                {
                    EmployeeCode = "EMP001",
                    FullName = "John Smith",
                    Position = "System Administrator",
                    DepartmentId = itDept.Id,
                    IsTeamLeader = true,
                    Salary = 25000000,
                    PhoneNumber = "0901234567",
                    Address = "123 Main Street, New York, NY",
                    HireDate = DateTime.UtcNow.AddYears(-3),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "EMP002",
                    FullName = "Jane Wilson",
                    Position = "Senior Developer",
                    DepartmentId = itDept.Id,
                    IsTeamLeader = false,
                    Salary = 20000000,
                    PhoneNumber = "0901234568",
                    Address = "456 Oak Avenue, New York, NY",
                    HireDate = DateTime.UtcNow.AddYears(-2),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "HR001",
                    FullName = "Sarah Johnson",
                    Position = "HR Manager",
                    DepartmentId = hrDept.Id,
                    IsTeamLeader = true,
                    Salary = 24000000,
                    PhoneNumber = "0901234570",
                    Address = "111 HR Street, New York, NY",
                    HireDate = DateTime.UtcNow.AddYears(-4),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Employees.AddRange(employees);
            await context.SaveChangesAsync();
            
            // Update department team leaders
            itDept.TeamLeaderId = "EMP001";
            hrDept.TeamLeaderId = "HR001";
            
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} employees", employees.Length);
        }
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var employees = await context.Employees.ToListAsync();

        foreach (var employee in employees)
        {
            var email = $"{employee.EmployeeCode.ToLower()}@sss.company.com";
            
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = employee.EmployeeCode,
                    Email = email,
                    EmailConfirmed = true,
                    EmployeeCode = employee.EmployeeCode,
                    FullName = employee.FullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "Password@123");
                
                if (result.Succeeded)
                {
                    // Assign roles based on position/department
                    if (employee.EmployeeCode == "EMP001")
                    {
                        await userManager.AddToRoleAsync(user, "Administrator");
                    }
                    else if (employee.IsTeamLeader)
                    {
                        await userManager.AddToRoleAsync(user, "TeamLeader");
                    }
                    else
                    {
                        await userManager.AddToRoleAsync(user, "Employee");
                    }
                    
                    logger.LogInformation("? Created user: {Email}", email);
                }
            }
        }
    }

    private static async Task SeedShiftTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.ShiftTemplates.AnyAsync())
        {
            var shiftTemplates = new[]
            {
                new ShiftTemplate
                {
                    Name = "Morning Administrative Shift",
                    Code = "MORNING_ADMIN",
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(17, 0),
                    BreakStartTime = new TimeOnly(12, 0),
                    BreakEndTime = new TimeOnly(13, 0),
                    AllowedLateMinutes = 15,
                    AllowedEarlyLeaveMinutes = 15,
                    StandardHours = 8.0m,
                    IsOvertimeEligible = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Description = "Standard administrative work shift 8AM-5PM"
                },
                new ShiftTemplate
                {
                    Name = "Flexible Shift",
                    Code = "FLEXIBLE",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(18, 0),
                    BreakStartTime = new TimeOnly(12, 30),
                    BreakEndTime = new TimeOnly(13, 30),
                    AllowedLateMinutes = 30,
                    AllowedEarlyLeaveMinutes = 30,
                    StandardHours = 8.0m,
                    IsOvertimeEligible = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Description = "Flexible work shift 9AM-6PM"
                }
            };

            context.ShiftTemplates.AddRange(shiftTemplates);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} shift templates", shiftTemplates.Length);
        }
    }

    private static async Task SeedHolidaysAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.Holidays.AnyAsync())
        {
            var currentYear = DateTime.Now.Year;
            var holidays = new[]
            {
                new Holiday
                {
                    Name = "New Year's Day",
                    HolidayDate = new DateTime(currentYear, 1, 1),
                    IsRecurring = true,
                    PayMultiplier = 2.0m,
                    HolidayType = "NATIONAL",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Description = "New Year's Day Holiday"
                },
                new Holiday
                {
                    Name = "Independence Day",
                    HolidayDate = new DateTime(currentYear, 7, 4),
                    IsRecurring = true,
                    PayMultiplier = 2.0m,
                    HolidayType = "NATIONAL",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Description = "Independence Day Holiday"
                }
            };

            context.Holidays.AddRange(holidays);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} holidays", holidays.Length);
        }
    }

    private static async Task SeedShiftAssignmentsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.ShiftAssignments.AnyAsync())
        {
            var employees = await context.Employees.Take(3).ToListAsync();
            var morningShift = await context.ShiftTemplates.FirstAsync(st => st.Code == "MORNING_ADMIN");

            var assignments = new List<ShiftAssignment>();
            
            foreach (var employee in employees)
            {
                assignments.Add(new ShiftAssignment
                {
                    EmployeeCode = employee.EmployeeCode,
                    ShiftTemplateId = morningShift.Id,
                    WorkLocationId = 1, // NY_MAIN
                    StartDate = DateTime.Today.AddDays(-30),
                    EndDate = null, // Indefinite
                    RecurrencePattern = "DAILY",
                    WeekDays = "1,2,3,4,5", // Monday to Friday
                    AssignedBy = "EMP001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Notes = $"Default shift assignment for {employee.FullName}"
                });
            }

            context.ShiftAssignments.AddRange(assignments);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} shift assignments", assignments.Count);
        }
    }

    private static async Task SeedSampleAttendanceDataAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.AttendanceEvents.AnyAsync())
        {
            var employees = await context.Employees.Take(2).ToListAsync();
            var events = new List<AttendanceEvent>();

            foreach (var employee in employees)
            {
                for (int i = 1; i <= 3; i++) // Last 3 days
                {
                    var workDay = DateTime.Today.AddDays(-i);
                    
                    // Skip weekends
                    if (workDay.DayOfWeek == DayOfWeek.Saturday || workDay.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    // Check-in event
                    events.Add(new AttendanceEvent
                    {
                        EmployeeCode = employee.EmployeeCode,
                        EventDateTime = workDay.AddHours(8).AddMinutes(Random.Shared.Next(-10, 20)),
                        EventType = "CHECK_IN",
                        WorkLocationId = 1,
                        DeviceInfo = "Web Browser",
                        IPAddress = "192.168.1.100",
                        IsManualEntry = true,
                        ApprovalStatus = "AUTO_APPROVED",
                        CreatedAt = workDay.AddHours(8),
                        Notes = "Morning check-in"
                    });

                    // Check-out event  
                    events.Add(new AttendanceEvent
                    {
                        EmployeeCode = employee.EmployeeCode,
                        EventDateTime = workDay.AddHours(17).AddMinutes(Random.Shared.Next(-20, 30)),
                        EventType = "CHECK_OUT",
                        WorkLocationId = 1,
                        DeviceInfo = "Web Browser",
                        IPAddress = "192.168.1.100",
                        IsManualEntry = true,
                        ApprovalStatus = "AUTO_APPROVED",
                        CreatedAt = workDay.AddHours(17),
                        Notes = "Evening check-out"
                    });
                }
            }

            context.AttendanceEvents.AddRange(events);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} sample attendance events", events.Count);
        }
    }

    private static async Task SeedPayrollPeriodsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.PayrollPeriods.AnyAsync())
        {
            var currentDate = DateTime.Today;
            var periods = new[]
            {
                new PayrollPeriod
                {
                    PeriodName = $"{currentDate:MMMM yyyy}",
                    StartDate = new DateTime(currentDate.Year, currentDate.Month, 1),
                    EndDate = new DateTime(currentDate.Year, currentDate.Month + 1, 1).AddDays(-1),
                    PeriodType = "MONTHLY",
                    Status = "OPEN",
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Current payroll period is open"
                }
            };

            context.PayrollPeriods.AddRange(periods);
            await context.SaveChangesAsync();
            logger.LogInformation("? Seeded {Count} payroll periods", periods.Length);
        }
    }
}