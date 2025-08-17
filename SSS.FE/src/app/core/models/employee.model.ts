import { BaseEntity } from './api-response.model';

// Employee interfaces matching backend DTOs exactly
export interface Employee {
  id: number;
  employeeCode: string;
  fullName: string;
  position?: string;
  phoneNumber?: string;
  address?: string;
  hireDate?: Date;
  salary?: number;
  isActive: boolean;
  isTeamLeader: boolean;
  createdAt: Date;
  updatedAt?: Date;

  // Department information
  departmentId?: number;
  departmentName?: string;
  departmentCode?: string;
}

// Employee creation/update DTO
export interface CreateEmployeeRequest {
  employeeCode: string;
  fullName: string;
  position?: string;
  phoneNumber?: string;
  address?: string;
  hireDate?: Date;
  salary?: number;
  departmentId?: number;
  isTeamLeader?: boolean;
}

export interface UpdateEmployeeRequest {
  fullName: string;
  position?: string;
  phoneNumber?: string;
  address?: string;
  hireDate?: Date;
  salary?: number;
  departmentId?: number;
  isTeamLeader?: boolean;
  isActive?: boolean;
}

// Employee list item for tables
export interface EmployeeListItem {
  id: number;
  employeeCode: string;
  fullName: string;
  position?: string;
  departmentName?: string;
  isActive: boolean;
  isTeamLeader: boolean;
}

// Employee filter options
export interface EmployeeFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  departmentId?: number;
  isTeamLeader?: boolean;
  includeInactive?: boolean;
}

// Employee statistics
export interface EmployeeStats {
  totalEmployees: number;
  activeEmployees: number;
  inactiveEmployees: number;
  teamLeaders: number;
  employeesByDepartment: EmployeesByDepartment[];
}

export interface EmployeesByDepartment {
  departmentName: string;
  count: number;
  percentage: number;
}

// Employee form validation
export interface EmployeeFormErrors {
  employeeCode?: string;
  fullName?: string;
  position?: string;
  phoneNumber?: string;
  address?: string;
  hireDate?: string;
  salary?: string;
  departmentId?: string;
}
