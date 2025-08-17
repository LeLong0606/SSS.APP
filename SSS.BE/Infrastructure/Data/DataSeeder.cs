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
                    PhoneNumber = "+1-555-0001",
                    Address = "123 Main St, City, State",
                    HireDate = DateTime.UtcNow.AddYears(-2),
                    Salary = 95000m,
                    DepartmentId = itDept?.Id,
                    IsTeamLeader = false, // Admin is not a team leader of any specific department
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "DIR001",
                    FullName = "John Smith",
                    Position = "Director",
                    PhoneNumber = "+1-555-0002",
                    Address = "456 Oak Ave, City, State",
                    HireDate = DateTime.UtcNow.AddYears(-3),
                    Salary = 120000m,
                    DepartmentId = null, // Director oversees multiple departments
                    IsTeamLeader = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Employee
                {
                    EmployeeCode = "TL001",
                    FullName = "Jane Doe",
                    Position = "IT Team Leader",
                    PhoneNumber = "+1-555-0003",
                    Address = "789 Pine St, City, State",
                    HireDate = DateTime.UtcNow.AddYears(-1),
                    Salary = 85000m,
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
                    PhoneNumber = "+1-555-0004",
                    Address = "321 Elm St, City, State",
                    HireDate = DateTime.UtcNow.AddMonths(-8),
                    Salary = 78000m,
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
                    PhoneNumber = "+1-555-0005",
                    Address = "654 Maple Ave, City, State",
                    HireDate = DateTime.UtcNow.AddMonths(-6),
                    Salary = 72000m,
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
                    PhoneNumber = "+1-555-0006",
                    Address = "987 Cedar Ln, City, State",
                    HireDate = DateTime.UtcNow.AddMonths(-4),
                    Salary = 58000m,
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
                    PhoneNumber = "+1-555-0007",
                    Address = "147 Birch St, City, State",
                    HireDate = DateTime.UtcNow.AddMonths(-3),
                    Salary = 65000m,
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
        }
    }
}