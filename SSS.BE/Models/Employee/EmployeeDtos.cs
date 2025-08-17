using System.ComponentModel.DataAnnotations;

namespace SSS.BE.Models.Employee;

// Employee DTOs
public class CreateEmployeeRequest
{
    [Required(ErrorMessage = "Employee code is required")]
    [MaxLength(50, ErrorMessage = "Employee code cannot exceed 50 characters")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
    public string? Position { get; set; }

    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }

    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    public DateTime? HireDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    public decimal? Salary { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsTeamLeader { get; set; } = false;
}

public class UpdateEmployeeRequest
{
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
    public string? Position { get; set; }

    [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }

    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    public DateTime? HireDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    public decimal? Salary { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsTeamLeader { get; set; } = false;

    public bool IsActive { get; set; } = true;
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal? Salary { get; set; }
    public bool IsActive { get; set; }
    public bool IsTeamLeader { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Department information
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentCode { get; set; }
}

// Department DTOs
public class CreateDepartmentRequest
{
    [Required(ErrorMessage = "Department name is required")]
    [MaxLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Department code cannot exceed 20 characters")]
    public string? DepartmentCode { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public string? TeamLeaderEmployeeCode { get; set; }
}

public class UpdateDepartmentRequest
{
    [Required(ErrorMessage = "Department name is required")]
    [MaxLength(100, ErrorMessage = "Department name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Department code cannot exceed 20 characters")]
    public string? DepartmentCode { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public string? TeamLeaderEmployeeCode { get; set; }

    public bool IsActive { get; set; } = true;
}

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DepartmentCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Team Leader information
    public string? TeamLeaderEmployeeCode { get; set; }
    public string? TeamLeaderFullName { get; set; }

    // Employee count
    public int EmployeeCount { get; set; }

    // Employees list (optional, for detailed view)
    public List<EmployeeDto>? Employees { get; set; }
}

// Common response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<string> Errors { get; set; } = new();
}