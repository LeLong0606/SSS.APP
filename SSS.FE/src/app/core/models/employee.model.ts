import { BaseEntity } from './api-response.model';

// Employee entity
export interface Employee extends BaseEntity {
  employeeCode: string;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  position?: string;
  departmentId?: string;
  department?: Department;
  workLocationId?: string;
  workLocation?: WorkLocation;
  isActive: boolean;
  isTeamLeader: boolean;
  hireDate?: Date;
  birthDate?: Date;
  avatar?: string;
  notes?: string;
}

// Department entity
export interface Department extends BaseEntity {
  departmentCode: string;
  name: string;
  description?: string;
  teamLeaderId?: string;
  teamLeader?: Employee;
  employees?: Employee[];
  isActive: boolean;
  employeeCount?: number;
}

// Work location entity
export interface WorkLocation extends BaseEntity {
  locationCode: string;
  name: string;
  address?: string;
  description?: string;
  isActive: boolean;
}

// Employee creation/update DTO
export interface CreateEmployeeRequest {
  employeeCode: string;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  position?: string;
  departmentId?: string;
  workLocationId?: string;
  isTeamLeader?: boolean;
  hireDate?: Date;
  birthDate?: Date;
  notes?: string;
}

export interface UpdateEmployeeRequest extends CreateEmployeeRequest {
  isActive?: boolean;
}

// Department creation/update DTO
export interface CreateDepartmentRequest {
  departmentCode: string;
  name: string;
  description?: string;
  teamLeaderId?: string;
}

export interface UpdateDepartmentRequest extends CreateDepartmentRequest {
  isActive?: boolean;
}

// Employee search/filter parameters
export interface EmployeeSearchParams {
  searchTerm?: string;
  departmentId?: string;
  workLocationId?: string;
  isActive?: boolean;
  isTeamLeader?: boolean;
  position?: string;
  dateFrom?: Date;
  dateTo?: Date;
}

// Department search/filter parameters
export interface DepartmentSearchParams {
  searchTerm?: string;
  isActive?: boolean;
  hasTeamLeader?: boolean;
}

// Employee statistics
export interface EmployeeStatistics {
  totalEmployees: number;
  activeEmployees: number;
  inactiveEmployees: number;
  teamLeadersCount: number;
  departmentDistribution: { [departmentName: string]: number };
  positionDistribution: { [position: string]: number };
}

// Department statistics
export interface DepartmentStatistics {
  totalDepartments: number;
  activeDepartments: number;
  averageEmployeesPerDepartment: number;
  departmentsWithTeamLeader: number;
}
