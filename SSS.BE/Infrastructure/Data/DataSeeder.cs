using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSS.BE.Infrastructure.Identity;
using SSS.BE.Persistence;
using SSS.BE.Domain.Entities;

namespace SSS.BE.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles - 4 basic English roles
        string[] roles = { "Administrator", "Director", "TeamLeader", "Employee" };
        
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        var adminEmail = "admin@sss.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmployeeCode = "ADMIN001",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }

        // Seed sample users for testing
        var sampleUsers = new[]
        {
            new { Email = "director@sss.com", FullName = "John Smith", Role = "Director", EmployeeCode = "DIR001", Password = "Director@123" },
            new { Email = "teamlead@sss.com", FullName = "Jane Doe", Role = "TeamLeader", EmployeeCode = "TL001", Password = "TeamLead@123" },
            new { Email = "employee@sss.com", FullName = "Bob Johnson", Role = "Employee", EmployeeCode = "EMP001", Password = "Employee@123" },
            new { Email = "teamlead2@sss.com", FullName = "Alice Wilson", Role = "TeamLeader", EmployeeCode = "TL002", Password = "TeamLead@123" },
            new { Email = "employee2@sss.com", FullName = "Charlie Brown", Role = "Employee", EmployeeCode = "EMP002", Password = "Employee@123" },
            new { Email = "employee3@sss.com", FullName = "Diana Prince", Role = "Employee", EmployeeCode = "EMP003", Password = "Employee@123" }
        };

        foreach (var sampleUser in sampleUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(sampleUser.Email);
            if (existingUser == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = sampleUser.Email,
                    Email = sampleUser.Email,
                    FullName = sampleUser.FullName,
                    EmployeeCode = sampleUser.EmployeeCode,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newUser, sampleUser.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, sampleUser.Role);
                }
            }
        }

        // Save changes for users first
        await context.SaveChangesAsync();

        // Seed Departments
        if (!await context.Departments.AnyAsync())
        {
            var departments = new[]
            {
                new Department
                {
                    Name = "Information Technology",
                    DepartmentCode = "IT",
                    Description = "Responsible for managing technology infrastructure and software development",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "Human Resources",
                    DepartmentCode = "HR",
                    Description = "Manages employee relations, recruitment, and organizational development",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "Finance",
                    DepartmentCode = "FIN",
                    Description = "Handles financial planning, accounting, and budget management",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Departments.AddRange(departments);
            await context.SaveChangesAsync();
        }

        // Seed Work Locations
        if (!await context.WorkLocations.AnyAsync())
        {
            var workLocations = new[]
            {
                new WorkLocation
                {
                    Name = "Main Office",
                    LocationCode = "MAIN",
                    Address = "123 Business District, Ho Chi Minh City",
                    Description = "Primary office location with all departments",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Branch Office",
                    LocationCode = "BRANCH",
                    Address = "456 District 2, Ho Chi Minh City",
                    Description = "Secondary office location",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Remote Work",
                    LocationCode = "REMOTE",
                    Address = "Work from home",
                    Description = "Remote work from employee's location",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Client Site A",
                    LocationCode = "CLIENT-A",
                    Address = "789 Client Street, District 1",
                    Description = "On-site work at client location",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new WorkLocation
                {
                    Name = "Training Center",
                    LocationCode = "TRAINING",
                    Address = "321 Training Complex, District 7",
                    Description = "Corporate training and meeting facility",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.WorkLocations.AddRange(workLocations);
            await context.SaveChangesAsync();
        }

        // Seed Employees with department relationships
        if (!await context.Employees.AnyAsync())
        {
            var departments = await context.Departments.ToListAsync();
            var itDept = departments.FirstOrDefault(d => d.DepartmentCode == "IT");
            var hrDept = departments.FirstOrDefault(d => d.DepartmentCode == "HR");
            var finDept = departments.FirstOrDefault(d => d.DepartmentCode == "FIN");

            var employees = new[]
            {
                new Employee
                {
                    EmployeeCode = "ADMIN001",
                    FullName = "System Administrator",
                    Position = "System Administrator",
                    PhoneNumber = "+84-901-234-567",
                    Address = "123 Main St, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddYears(-2),
                    Salary = 95000000m, // VND
                    DepartmentId = itDept?.Id,
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "DIR001",
                    FullName = "John Smith",
                    Position = "Director",
                    PhoneNumber = "+84-901-234-568",
                    Address = "456 Oak Ave, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddYears(-3),
                    Salary = 120000000m, // VND
                    DepartmentId = null,
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "TL001",
                    FullName = "Jane Doe",
                    Position = "IT Team Leader",
                    PhoneNumber = "+84-901-234-569",
                    Address = "789 Pine St, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddYears(-1),
                    Salary = 85000000m, // VND
                    DepartmentId = itDept?.Id,
                    IsTeamLeader = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "TL002",
                    FullName = "Alice Wilson",
                    Position = "HR Team Leader",
                    PhoneNumber = "+84-901-234-570",
                    Address = "321 Elm St, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddMonths(-8),
                    Salary = 78000000m, // VND
                    DepartmentId = hrDept?.Id,
                    IsTeamLeader = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "EMP001",
                    FullName = "Bob Johnson",
                    Position = "Software Developer",
                    PhoneNumber = "+84-901-234-571",
                    Address = "654 Maple Ave, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddMonths(-6),
                    Salary = 72000000m, // VND
                    DepartmentId = itDept?.Id,
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "EMP002",
                    FullName = "Charlie Brown",
                    Position = "HR Specialist",
                    PhoneNumber = "+84-901-234-572",
                    Address = "987 Cedar Ln, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddMonths(-4),
                    Salary = 58000000m, // VND
                    DepartmentId = hrDept?.Id,
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "EMP003",
                    FullName = "Diana Prince",
                    Position = "Financial Analyst",
                    PhoneNumber = "+84-901-234-573",
                    Address = "147 Birch St, Ho Chi Minh City",
                    HireDate = DateTime.UtcNow.AddMonths(-3),
                    Salary = 65000000m, // VND
                    DepartmentId = finDept?.Id,
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Employees.AddRange(employees);
            await context.SaveChangesAsync();

            // Update departments with team leaders
            if (itDept != null)
            {
                var itTeamLeader = employees.FirstOrDefault(e => e.EmployeeCode == "TL001");
                if (itTeamLeader != null)
                {
                    itDept.TeamLeaderId = itTeamLeader.EmployeeCode;
                    itDept.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (hrDept != null)
            {
                var hrTeamLeader = employees.FirstOrDefault(e => e.EmployeeCode == "TL002");
                if (hrTeamLeader != null)
                {
                    hrDept.TeamLeaderId = hrTeamLeader.EmployeeCode;
                    hrDept.UpdatedAt = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync();

            // Seed sample work shifts for this week (Monday to Sunday)
            var workLocations = await context.WorkLocations.ToListAsync();
            var mainOffice = workLocations.FirstOrDefault(w => w.LocationCode == "MAIN");
            var remoteWork = workLocations.FirstOrDefault(w => w.LocationCode == "REMOTE");

            if (mainOffice != null && remoteWork != null)
            {
                var currentWeekMonday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
                
                var sampleShifts = new List<WorkShift>();

                // Create shifts for TL001 (Jane Doe) - IT Team Leader
                for (int day = 0; day < 5; day++) // Monday to Friday
                {
                    sampleShifts.Add(new WorkShift
                    {
                        EmployeeCode = "TL001",
                        WorkLocationId = day < 3 ? mainOffice.Id : remoteWork.Id, // 3 days office, 2 days remote
                        ShiftDate = currentWeekMonday.AddDays(day),
                        StartTime = new TimeOnly(8, 0), // 8:00 AM
                        EndTime = new TimeOnly(17, 0), // 5:00 PM (8 hours with 1 hour break)
                        TotalHours = 8.0m,
                        AssignedByEmployeeCode = "TL001", // Self-assigned
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                // Create shifts for EMP001 (Bob Johnson) - assigned by TL001
                for (int day = 0; day < 5; day++) // Monday to Friday
                {
                    sampleShifts.Add(new WorkShift
                    {
                        EmployeeCode = "EMP001",
                        WorkLocationId = day == 4 ? remoteWork.Id : mainOffice.Id, // Friday remote
                        ShiftDate = currentWeekMonday.AddDays(day),
                        StartTime = new TimeOnly(9, 0), // 9:00 AM
                        EndTime = new TimeOnly(18, 0), // 6:00 PM (8 hours with 1 hour break)
                        TotalHours = 8.0m,
                        AssignedByEmployeeCode = "TL001", // Assigned by team leader
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                // Create shifts for TL002 (Alice Wilson) - HR Team Leader
                for (int day = 0; day < 5; day++) // Monday to Friday
                {
                    sampleShifts.Add(new WorkShift
                    {
                        EmployeeCode = "TL002",
                        WorkLocationId = mainOffice.Id, // Always in office
                        ShiftDate = currentWeekMonday.AddDays(day),
                        StartTime = new TimeOnly(8, 30), // 8:30 AM
                        EndTime = new TimeOnly(17, 30), // 5:30 PM (8 hours with 1 hour break)
                        TotalHours = 8.0m,
                        AssignedByEmployeeCode = "TL002", // Self-assigned
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                // Create shifts for EMP002 (Charlie Brown) - assigned by TL002
                for (int day = 0; day < 5; day++) // Monday to Friday
                {
                    sampleShifts.Add(new WorkShift
                    {
                        EmployeeCode = "EMP002",
                        WorkLocationId = mainOffice.Id,
                        ShiftDate = currentWeekMonday.AddDays(day),
                        StartTime = new TimeOnly(8, 0), // 8:00 AM
                        EndTime = new TimeOnly(16, 0), // 4:00 PM (7 hours)
                        TotalHours = 7.0m,
                        AssignedByEmployeeCode = "TL002", // Assigned by team leader
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                context.WorkShifts.AddRange(sampleShifts);
                await context.SaveChangesAsync();

                // Create sample work shift logs for demonstration
                var sampleLogs = new List<WorkShiftLog>();

                foreach (var shift in sampleShifts)
                {
                    sampleLogs.Add(new WorkShiftLog
                    {
                        WorkShiftId = shift.Id,
                        Action = "CREATE",
                        PerformedByEmployeeCode = shift.AssignedByEmployeeCode,
                        PerformedAt = shift.CreatedAt,
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            shift.EmployeeCode,
                            shift.WorkLocationId,
                            shift.ShiftDate,
                            shift.StartTime,
                            shift.EndTime,
                            shift.TotalHours
                        }),
                        Comments = shift.AssignedByEmployeeCode == shift.EmployeeCode 
                            ? "Self-assigned shift" 
                            : "Assigned by team leader"
                    });
                }

                context.WorkShiftLogs.AddRange(sampleLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}