import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { WorkShift, CreateWorkShiftRequest, UpdateWorkShiftRequest, WeeklyShiftRequest } from '../models/work-shift.model';

@Injectable({
  providedIn: 'root'
})
export class WorkShiftService {
  private readonly API_URL = `${environment.apiUrl}/workshift`;

  constructor(private http: HttpClient) {}

  // Get all work shifts with pagination
  getWorkShifts(
    page: number = 1, 
    pageSize: number = 10, 
    employeeCode?: string,
    startDate?: string,
    endDate?: string,
    locationId?: number // ✅ FIX: NUMBER, not string!
  ): Observable<PagedResponse<WorkShift>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (employeeCode) params = params.set('employeeCode', employeeCode);
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (locationId) params = params.set('locationId', locationId.toString()); // Convert number to string for HTTP param

    return this.http.get<PagedResponse<WorkShift>>(this.API_URL, { params });
  }

  // Get work shift by ID
  getWorkShift(id: number): Observable<ApiResponse<WorkShift>> { // ✅ FIX: NUMBER parameter
    return this.http.get<ApiResponse<WorkShift>>(`${this.API_URL}/${id}`);
  }

  // ✅ FIXED: Create new work shift with proper data formatting
  createWorkShift(workShift: CreateWorkShiftRequest): Observable<ApiResponse<WorkShift>> {
    // ✅ FIX: Ensure data types match backend expectations exactly
    const createData = {
      employeeCode: workShift.employeeCode,
      workLocationId: Number(workShift.workLocationId), // Ensure number
      shiftDate: workShift.shiftDate instanceof Date 
        ? workShift.shiftDate.toISOString().split('T')[0] // Convert Date to YYYY-MM-DD string for API
        : workShift.shiftDate,
      startTime: workShift.startTime, // HH:mm format
      endTime: workShift.endTime // HH:mm format
    };
    
    return this.http.post<ApiResponse<WorkShift>>(this.API_URL, createData);
  }

  // ✅ FIXED: Update work shift with proper ID handling
  updateWorkShift(id: number, workShift: UpdateWorkShiftRequest): Observable<ApiResponse<WorkShift>> {
    // ✅ FIX: Ensure workLocationId is number
    const updateData = {
      workLocationId: Number(workShift.workLocationId),
      startTime: workShift.startTime,
      endTime: workShift.endTime,
      modificationReason: workShift.modificationReason
    };
    
    return this.http.put<ApiResponse<WorkShift>>(`${this.API_URL}/${id}`, updateData);
  }

  // ✅ FIXED: Delete work shift with number ID
  deleteWorkShift(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.API_URL}/${id}`);
  }

  // Get weekly shifts for employee
  getWeeklyShifts(employeeCode: string, weekStart?: string): Observable<ApiResponse<WorkShift[]>> {
    let url = `${this.API_URL}/weekly/${employeeCode}`;
    
    if (weekStart) {
      const params = new HttpParams().set('weekStart', weekStart);
      return this.http.get<ApiResponse<WorkShift[]>>(url, { params });
    }
    
    return this.http.get<ApiResponse<WorkShift[]>>(url);
  }

  // ✅ FIXED: Create weekly shifts with proper data formatting
  createWeeklyShifts(weeklyShift: WeeklyShiftRequest): Observable<ApiResponse<WorkShift[]>> {
    // ✅ FIX: Ensure all data types match backend expectations
    const createData = {
      employeeCode: weeklyShift.employeeCode,
      weekStartDate: weeklyShift.weekStartDate instanceof Date 
        ? weeklyShift.weekStartDate.toISOString().split('T')[0]
        : weeklyShift.weekStartDate,
      dailyShifts: weeklyShift.dailyShifts.map(shift => ({
        dayOfWeek: shift.dayOfWeek,
        workLocationId: Number(shift.workLocationId), // Ensure number
        startTime: shift.startTime,
        endTime: shift.endTime
      }))
    };
    
    return this.http.post<ApiResponse<WorkShift[]>>(`${this.API_URL}/weekly`, createData);
  }

  // Get shifts by date range
  getShiftsByDateRange(startDate: string, endDate: string, employeeCode?: string): Observable<ApiResponse<WorkShift[]>> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);

    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }

    return this.http.get<ApiResponse<WorkShift[]>>(`${this.API_URL}/range`, { params });
  }

  // ✅ FIXED: Get shifts by location with number ID
  getShiftsByLocation(locationId: number, date?: string): Observable<ApiResponse<WorkShift[]>> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);

    return this.http.get<ApiResponse<WorkShift[]>>(`${this.API_URL}/location/${locationId}`, { params });
  }

  // Get employee schedule
  getEmployeeSchedule(employeeCode: string, month?: number, year?: number): Observable<ApiResponse<WorkShift[]>> {
    let params = new HttpParams();
    if (month) params = params.set('month', month.toString());
    if (year) params = params.set('year', year.toString());

    return this.http.get<ApiResponse<WorkShift[]>>(`${this.API_URL}/employee/${employeeCode}/schedule`, { params });
  }

  // Get work shift statistics
  getWorkShiftStats(startDate?: string, endDate?: string): Observable<ApiResponse<any>> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`, { params });
  }

  // ✅ FIXED: Bulk assign shifts with proper data formatting
  bulkAssignShifts(shifts: CreateWorkShiftRequest[]): Observable<ApiResponse<WorkShift[]>> {
    // ✅ FIX: Ensure all shifts have proper data types
    const formattedShifts = shifts.map(shift => ({
      employeeCode: shift.employeeCode,
      workLocationId: Number(shift.workLocationId),
      shiftDate: shift.shiftDate instanceof Date 
        ? shift.shiftDate.toISOString().split('T')[0]
        : shift.shiftDate,
      startTime: shift.startTime,
      endTime: shift.endTime
    }));

    return this.http.post<ApiResponse<WorkShift[]>>(`${this.API_URL}/bulk-assign`, { shifts: formattedShifts });
  }

  // Copy shifts from one week to another
  copyWeeklyShifts(fromWeek: string, toWeek: string, employeeCode?: string): Observable<ApiResponse<WorkShift[]>> {
    const body = {
      fromWeek,
      toWeek,
      employeeCode
    };

    return this.http.post<ApiResponse<WorkShift[]>>(`${this.API_URL}/copy-week`, body);
  }

  // Export shifts to Excel
  exportShifts(startDate?: string, endDate?: string, employeeCode?: string): Observable<Blob> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (employeeCode) params = params.set('employeeCode', employeeCode);

    return this.http.get(`${this.API_URL}/export`, { 
      params,
      responseType: 'blob',
      headers: { 'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }
    });
  }

  // ✅ FIXED: Get shift conflicts with proper parameter types
  getShiftConflicts(employeeCode: string, date: string, startTime: string, endTime: string): Observable<ApiResponse<WorkShift[]>> {
    const params = new HttpParams()
      .set('employeeCode', employeeCode)
      .set('date', date)
      .set('startTime', startTime)
      .set('endTime', endTime);

    return this.http.get<ApiResponse<WorkShift[]>>(`${this.API_URL}/conflicts`, { params });
  }
}
