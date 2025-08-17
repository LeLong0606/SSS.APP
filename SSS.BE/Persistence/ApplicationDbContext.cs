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

    // New DbSets for Department and Employee
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode)
                  .IsUnique()
                  .HasDatabaseName("IX_ApplicationUser_EmployeeCode");
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
        });

        // Configure Employee entity
        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            
            // Unique constraint on EmployeeCode
            entity.HasIndex(e => e.EmployeeCode)
                  .IsUnique()
                  .HasDatabaseName("IX_Employee_EmployeeCode");

            // Configure relationship with Department
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision for Salary
            entity.Property(e => e.Salary)
                  .HasPrecision(18, 2);
        });

        // Configure Department entity
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            
            // Unique constraint on DepartmentCode if provided
            entity.HasIndex(d => d.DepartmentCode)
                  .IsUnique()
                  .HasDatabaseName("IX_Department_DepartmentCode")
                  .HasFilter("[DepartmentCode] IS NOT NULL");

            // Configure Team Leader relationship using EmployeeCode as principal key
            entity.HasOne(d => d.TeamLeader)
                  .WithOne()
                  .HasForeignKey<Department>(d => d.TeamLeaderId)
                  .HasPrincipalKey<Employee>(e => e.EmployeeCode)
                  .OnDelete(DeleteBehavior.SetNull);
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