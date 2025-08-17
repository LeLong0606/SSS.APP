import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { Department, CreateDepartmentRequest, UpdateDepartmentRequest, DepartmentFilter } from '../models/department.model';
import { Employee, EmployeeFilter } from '../models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private readonly API_URL = `${environment.apiUrl}/department`;

  constructor(private http: HttpClient) {}

  // Get all departments with pagination - matches GET /api/department
  getDepartments(filter: DepartmentFilter = {}): Observable<PagedResponse<Department>> {
    let params = new HttpParams();
    
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);
    if (filter.includeEmployees) params = params.set('includeEmployees', filter.includeEmployees.toString());

    return this.http.get<PagedResponse<Department>>(this.API_URL, { params });
  }

  // Get department by ID - matches GET /api/department/{id}
  getDepartment(id: number, includeEmployees: boolean = false): Observable<ApiResponse<Department>> {
    let params = new HttpParams();
    if (includeEmployees) params = params.set('includeEmployees', 'true');
    
    return this.http.get<ApiResponse<Department>>(`${this.API_URL}/${id}`, { params });
  }

  // Create new department - matches POST /api/department (Requires: Director+)
  createDepartment(request: CreateDepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.post<ApiResponse<Department>>(this.API_URL, request);
  }

  // Update department - matches PUT /api/department/{id} (Requires: Director+)
  updateDepartment(id: number, request: UpdateDepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.put<ApiResponse<Department>>(`${this.API_URL}/${id}`, request);
  }

  // Delete department - matches DELETE /api/department/{id} (Requires: Administrator)
  deleteDepartment(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/${id}`);
  }

  // Get department employees - matches GET /api/department/{id}/employees
  getDepartmentEmployees(id: number, filter: EmployeeFilter = {}): Observable<PagedResponse<Employee>> {
    let params = new HttpParams();
    
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.search) params = params.set('search', filter.search);

    return this.http.get<PagedResponse<Employee>>(`${this.API_URL}/${id}/employees`, { params });
  }

  // Assign team leader - matches POST /api/department/{id}/assign-team-leader (Requires: Director+)
  assignTeamLeader(departmentId: number, employeeCode: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/${departmentId}/assign-team-leader`, employeeCode, {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  // Remove team leader - matches DELETE /api/department/{id}/remove-team-leader (Requires: Director+)
  removeTeamLeader(departmentId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/${departmentId}/remove-team-leader`);
  }

  // Convenience methods
  getAllDepartments(): Observable<PagedResponse<Department>> {
    return this.getDepartments({ pageSize: 100 }); // Get a large page size to get all departments
  }

  getActiveDepartments(): Observable<PagedResponse<Department>> {
    return this.getDepartments({ includeInactive: false });
  }

  searchDepartments(searchTerm: string, pageNumber: number = 1, pageSize: number = 10): Observable<PagedResponse<Department>> {
    return this.getDepartments({
      search: searchTerm,
      pageNumber,
      pageSize
    });
  }

  // Get department with employees
  getDepartmentWithEmployees(id: number): Observable<ApiResponse<Department>> {
    return this.getDepartment(id, true);
  }

  // Get department statistics
  getDepartmentStats(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`);
  }
}
