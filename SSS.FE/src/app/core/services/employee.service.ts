import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { Employee, CreateEmployeeRequest, UpdateEmployeeRequest, EmployeeFilter } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly API_URL = `${environment.apiUrl}/employee`;

  constructor(private http: HttpClient) {}

  // Get all employees with pagination and filters - matches GET /api/employee
  getEmployees(filter: EmployeeFilter = {}): Observable<PagedResponse<Employee>> {
    let params = new HttpParams();
    
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.departmentId) params = params.set('departmentId', filter.departmentId.toString());
    if (filter.isTeamLeader !== undefined) params = params.set('isTeamLeader', filter.isTeamLeader.toString());

    return this.http.get<PagedResponse<Employee>>(this.API_URL, { params });
  }

  // Get employee by ID - matches GET /api/employee/{id}
  getEmployee(id: number): Observable<ApiResponse<Employee>> {
    return this.http.get<ApiResponse<Employee>>(`${this.API_URL}/${id}`);
  }

  // Get employee by employee code - matches GET /api/employee/code/{employeeCode}
  getEmployeeByCode(employeeCode: string): Observable<ApiResponse<Employee>> {
    return this.http.get<ApiResponse<Employee>>(`${this.API_URL}/code/${employeeCode}`);
  }

  // Create new employee - matches POST /api/employee (Requires: TeamLeader+)
  createEmployee(request: CreateEmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.post<ApiResponse<Employee>>(this.API_URL, request);
  }

  // Update employee - matches PUT /api/employee/{id} (Requires: TeamLeader+)
  updateEmployee(id: number, request: UpdateEmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${this.API_URL}/${id}`, request);
  }

  // Delete employee - matches DELETE /api/employee/{id} (Requires: Director+)
  deleteEmployee(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/${id}`);
  }

  // Toggle employee status - matches PATCH /api/employee/{id}/status (Requires: Director+)
  toggleEmployeeStatus(id: number, isActive: boolean): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.API_URL}/${id}/status`, isActive);
  }

  // Get employee statistics
  getEmployeeStats(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`);
  }

  // Convenience methods for common use cases
  getActiveEmployees(filter: EmployeeFilter = {}): Observable<PagedResponse<Employee>> {
    return this.getEmployees({ ...filter, includeInactive: false });
  }

  getTeamLeaders(filter: EmployeeFilter = {}): Observable<PagedResponse<Employee>> {
    return this.getEmployees({ ...filter, isTeamLeader: true });
  }

  searchEmployees(searchTerm: string, pageNumber: number = 1, pageSize: number = 10): Observable<PagedResponse<Employee>> {
    return this.getEmployees({
      search: searchTerm,
      pageNumber,
      pageSize
    });
  }

  // Get employees by department
  getEmployeesByDepartment(departmentId: number, filter: EmployeeFilter = {}): Observable<PagedResponse<Employee>> {
    return this.getEmployees({ ...filter, departmentId });
  }
}
