import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { WorkLocation, CreateWorkLocationRequest, UpdateWorkLocationRequest } from '../models/work-location.model';

@Injectable({
  providedIn: 'root'
})
export class WorkLocationService {
  private readonly API_URL = `${environment.apiUrl}/worklocation`;

  constructor(private http: HttpClient) {}

  // Get all work locations with pagination
  getWorkLocations(page: number = 1, pageSize: number = 10, searchTerm?: string): Observable<PagedResponse<WorkLocation>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('search', searchTerm);
    }

    return this.http.get<PagedResponse<WorkLocation>>(this.API_URL, { params });
  }

  // Get all work locations (no pagination)
  getAllWorkLocations(): Observable<ApiResponse<WorkLocation[]>> {
    return this.http.get<ApiResponse<WorkLocation[]>>(`${this.API_URL}/all`);
  }

  // Get work location by ID
  getWorkLocation(id: string): Observable<ApiResponse<WorkLocation>> {
    return this.http.get<ApiResponse<WorkLocation>>(`${this.API_URL}/${id}`);
  }

  // ✅ FIX: Add alias method for backward compatibility
  getWorkLocationById(id: string | number): Observable<ApiResponse<WorkLocation>> {
    const stringId = typeof id === 'number' ? id.toString() : id;
    return this.getWorkLocation(stringId);
  }

  // Create new work location
  createWorkLocation(workLocation: CreateWorkLocationRequest): Observable<ApiResponse<WorkLocation>> {
    return this.http.post<ApiResponse<WorkLocation>>(this.API_URL, workLocation);
  }

  // Update work location - ✅ FIX: Accept both string and number ID
  updateWorkLocation(id: string | number, workLocation: UpdateWorkLocationRequest): Observable<ApiResponse<WorkLocation>> {
    const stringId = typeof id === 'number' ? id.toString() : id;
    return this.http.put<ApiResponse<WorkLocation>>(`${this.API_URL}/${stringId}`, workLocation);
  }

  // Delete work location - ✅ FIX: Accept both string and number ID
  deleteWorkLocation(id: string | number): Observable<ApiResponse<void>> {
    const stringId = typeof id === 'number' ? id.toString() : id;
    return this.http.delete<ApiResponse<void>>(`${this.API_URL}/${stringId}`);
  }

  // Search work locations
  searchWorkLocations(searchTerm: string): Observable<ApiResponse<WorkLocation[]>> {
    const params = new HttpParams().set('search', searchTerm);
    return this.http.get<ApiResponse<WorkLocation[]>>(`${this.API_URL}/search`, { params });
  }

  // Get work location with capacity information
  getWorkLocationWithCapacity(id: number): Observable<ApiResponse<WorkLocation>> {
    return this.getWorkLocation(id.toString());
  }

  // Get work location statistics
  getWorkLocationStats(): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`);
  }

  // Get active work locations
  getActiveWorkLocations(): Observable<ApiResponse<WorkLocation[]>> {
    return this.http.get<ApiResponse<WorkLocation[]>>(`${this.API_URL}/active`);
  }

  // Activate/Deactivate work location - ✅ FIX: Accept both string and number ID
  toggleWorkLocationStatus(id: string | number, isActive: boolean): Observable<ApiResponse<WorkLocation>> {
    const stringId = typeof id === 'number' ? id.toString() : id;
    return this.http.patch<ApiResponse<WorkLocation>>(`${this.API_URL}/${stringId}/status`, { isActive });
  }

  // Get work locations by location type
  getWorkLocationsByType(locationType: string): Observable<ApiResponse<WorkLocation[]>> {
    const params = new HttpParams().set('type', locationType);
    return this.http.get<ApiResponse<WorkLocation[]>>(`${this.API_URL}/by-type`, { params });
  }

  // Get work locations with capacity
  getWorkLocationsWithCapacity(minCapacity?: number): Observable<ApiResponse<WorkLocation[]>> {
    let params = new HttpParams();
    if (minCapacity) {
      params = params.set('minCapacity', minCapacity.toString());
    }

    return this.http.get<ApiResponse<WorkLocation[]>>(`${this.API_URL}/with-capacity`, { params });
  }
}
