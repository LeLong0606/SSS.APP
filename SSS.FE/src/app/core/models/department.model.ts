// Department interfaces matching backend DTOs exactly
export interface Department {
  id: number;
  name: string;
  departmentCode?: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
  
  // Team Leader information
  teamLeaderEmployeeCode?: string;
  teamLeaderFullName?: string;
  
  // Employee count
  employeeCount: number;
  
  // Employees list (optional, for detailed view)
  employees?: Employee[];
}

export interface CreateDepartmentRequest {
  name: string;
  departmentCode?: string;
  description?: string;
  teamLeaderEmployeeCode?: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  departmentCode?: string;
  description?: string;
  teamLeaderEmployeeCode?: string;
  isActive?: boolean;
}

// Department list item for tables
export interface DepartmentListItem {
  id: number;
  name: string;
  departmentCode?: string;
  employeeCount: number;
  teamLeaderFullName?: string;
  isActive: boolean;
}

// Department filter options
export interface DepartmentFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  includeEmployees?: boolean;
  includeInactive?: boolean;
}

// Department statistics
export interface DepartmentStats {
  totalDepartments: number;
  activeDepartments: number;
  inactiveDepartments: number;
  departmentsWithLeaders: number;
  averageEmployeesPerDepartment: number;
  departmentEmployeeCounts: DepartmentEmployeeCount[];
}

export interface DepartmentEmployeeCount {
  departmentName: string;
  employeeCount: number;
  percentage: number;
}

// Department form validation
export interface DepartmentFormErrors {
  name?: string;
  departmentCode?: string;
  description?: string;
  teamLeaderEmployeeCode?: string;
}

// Import Employee interface for employees property
import { Employee } from './employee.model';
