import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';

// Shift Management Models matching backend DTOs
export interface ShiftTemplateDto {
  id: number;
  name: string;
  code: string;
  startTime: string; // HH:mm format
  endTime: string; // HH:mm format
  breakStartTime?: string;
  breakEndTime?: string;
  allowedLateMinutes: number;
  allowedEarlyLeaveMinutes: number;
  standardHours: number;
  isOvertimeEligible: boolean;
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
  description?: string;
}

export interface CreateShiftTemplateRequest {
  name: string;
  code: string;
  startTime: string; // HH:mm:ss format
  endTime: string; // HH:mm:ss format
  breakStartTime?: string;
  breakEndTime?: string;
  allowedLateMinutes: number;
  allowedEarlyLeaveMinutes: number;
  standardHours: number;
  isOvertimeEligible: boolean;
  description?: string;
}

export interface UpdateShiftTemplateRequest extends CreateShiftTemplateRequest {
  isActive: boolean;
}

export interface ShiftAssignmentDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  shiftTemplateId: number;
  shiftTemplateName: string;
  workLocationId?: number;
  workLocationName?: string;
  startDate: Date;
  endDate?: Date;
  recurrencePattern: string;
  weekDays?: string;
  assignedBy: string;
  assignedByName: string;
  isActive: boolean;
  createdAt: Date;
  notes?: string;
}

export interface CreateShiftAssignmentRequest {
  employeeCode: string;
  shiftTemplateId: number;
  workLocationId?: number;
  startDate: Date;
  endDate?: Date;
  recurrencePattern: string;
  weekDays?: string;
  notes?: string;
}

export interface BulkShiftAssignmentRequest {
  employeeCodes: string[];
  shiftTemplateId: number;
  workLocationId?: number;
  startDate: Date;
  endDate?: Date;
  recurrencePattern: string;
  weekDays?: string;
  notes?: string;
}

export interface ShiftCalendarDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  shiftAssignmentId: number;
  shiftTemplateId: number;
  shiftTemplateName: string;
  workLocationId?: number;
  workLocationName?: string;
  shiftDate: Date;
  startTime: string;
  endTime: string;
  standardHours: number;
  shiftStatus: string;
  isHoliday: boolean;
  holidayName?: string;
  notes?: string;
}

export interface ShiftCalendarFilter {
  employeeCode?: string;
  startDate?: Date;
  endDate?: Date;
  shiftTemplateId?: number;
  workLocationId?: number;
  shiftStatus?: string;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ShiftManagementService {
  private readonly API_URL = `${environment.apiUrl}/shiftmanagement`;

  constructor(private http: HttpClient) {}

  // Shift Templates Management
  getShiftTemplates(
    pageNumber: number = 1,
    pageSize: number = 20,
    isActive?: boolean
  ): Observable<PagedResponse<ShiftTemplateDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (isActive !== undefined) {
      params = params.set('isActive', isActive.toString());
    }

    return this.http.get<PagedResponse<ShiftTemplateDto>>(`${this.API_URL}/templates`, { params });
  }

  getShiftTemplate(id: number): Observable<ApiResponse<ShiftTemplateDto>> {
    return this.http.get<ApiResponse<ShiftTemplateDto>>(`${this.API_URL}/templates/${id}`);
  }

  createShiftTemplate(template: CreateShiftTemplateRequest): Observable<ApiResponse<ShiftTemplateDto>> {
    return this.http.post<ApiResponse<ShiftTemplateDto>>(`${this.API_URL}/templates`, template);
  }

  updateShiftTemplate(id: number, template: UpdateShiftTemplateRequest): Observable<ApiResponse<ShiftTemplateDto>> {
    return this.http.put<ApiResponse<ShiftTemplateDto>>(`${this.API_URL}/templates/${id}`, template);
  }

  deleteShiftTemplate(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/templates/${id}`);
  }

  toggleShiftTemplateStatus(id: number): Observable<ApiResponse<ShiftTemplateDto>> {
    return this.http.patch<ApiResponse<ShiftTemplateDto>>(`${this.API_URL}/templates/${id}/toggle-status`, {});
  }

  // Shift Assignments Management
  getShiftAssignments(
    pageNumber: number = 1,
    pageSize: number = 20,
    employeeCode?: string,
    shiftTemplateId?: number,
    isActive?: boolean
  ): Observable<PagedResponse<ShiftAssignmentDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }
    if (shiftTemplateId !== undefined) {
      params = params.set('shiftTemplateId', shiftTemplateId.toString());
    }
    if (isActive !== undefined) {
      params = params.set('isActive', isActive.toString());
    }

    return this.http.get<PagedResponse<ShiftAssignmentDto>>(`${this.API_URL}/assignments`, { params });
  }

  getShiftAssignment(id: number): Observable<ApiResponse<ShiftAssignmentDto>> {
    return this.http.get<ApiResponse<ShiftAssignmentDto>>(`${this.API_URL}/assignments/${id}`);
  }

  createShiftAssignment(assignment: CreateShiftAssignmentRequest): Observable<ApiResponse<ShiftAssignmentDto>> {
    const assignmentData = {
      ...assignment,
      startDate: assignment.startDate.toISOString().split('T')[0],
      endDate: assignment.endDate?.toISOString().split('T')[0]
    };

    return this.http.post<ApiResponse<ShiftAssignmentDto>>(`${this.API_URL}/assignments`, assignmentData);
  }

  updateShiftAssignment(id: number, assignment: CreateShiftAssignmentRequest): Observable<ApiResponse<ShiftAssignmentDto>> {
    const assignmentData = {
      ...assignment,
      startDate: assignment.startDate.toISOString().split('T')[0],
      endDate: assignment.endDate?.toISOString().split('T')[0]
    };

    return this.http.put<ApiResponse<ShiftAssignmentDto>>(`${this.API_URL}/assignments/${id}`, assignmentData);
  }

  deleteShiftAssignment(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/assignments/${id}`);
  }

  // Bulk Shift Assignment
  bulkAssignShifts(request: BulkShiftAssignmentRequest): Observable<ApiResponse<ShiftAssignmentDto[]>> {
    const bulkData = {
      ...request,
      startDate: request.startDate.toISOString().split('T')[0],
      endDate: request.endDate?.toISOString().split('T')[0]
    };

    return this.http.post<ApiResponse<ShiftAssignmentDto[]>>(`${this.API_URL}/assignments/bulk`, bulkData);
  }

  // Shift Calendar Management
  getShiftCalendar(filter: ShiftCalendarFilter): Observable<PagedResponse<ShiftCalendarDto>> {
    let params = new HttpParams()
      .set('pageNumber', (filter.pageNumber || 1).toString())
      .set('pageSize', (filter.pageSize || 50).toString());

    if (filter.employeeCode) {
      params = params.set('employeeCode', filter.employeeCode);
    }
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString().split('T')[0]);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString().split('T')[0]);
    }
    if (filter.shiftTemplateId !== undefined) {
      params = params.set('shiftTemplateId', filter.shiftTemplateId.toString());
    }
    if (filter.workLocationId !== undefined) {
      params = params.set('workLocationId', filter.workLocationId.toString());
    }
    if (filter.shiftStatus) {
      params = params.set('shiftStatus', filter.shiftStatus);
    }

    return this.http.get<PagedResponse<ShiftCalendarDto>>(`${this.API_URL}/calendar`, { params });
  }

  getEmployeeShiftCalendar(
    employeeCode: string,
    startDate: Date,
    endDate: Date
  ): Observable<ApiResponse<ShiftCalendarDto[]>> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString().split('T')[0])
      .set('endDate', endDate.toISOString().split('T')[0]);

    return this.http.get<ApiResponse<ShiftCalendarDto[]>>(`${this.API_URL}/calendar/employee/${employeeCode}`, { params });
  }

  getMyShiftCalendar(startDate: Date, endDate: Date): Observable<ApiResponse<ShiftCalendarDto[]>> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString().split('T')[0])
      .set('endDate', endDate.toISOString().split('T')[0]);

    return this.http.get<ApiResponse<ShiftCalendarDto[]>>(`${this.API_URL}/calendar/my`, { params });
  }

  // Shift Generation
  generateShiftCalendar(
    startDate: Date,
    endDate: Date,
    employeeCode?: string
  ): Observable<ApiResponse<any>> {
    const generateData = {
      startDate: startDate.toISOString().split('T')[0],
      endDate: endDate.toISOString().split('T')[0],
      employeeCode: employeeCode
    };

    return this.http.post<ApiResponse<any>>(`${this.API_URL}/calendar/generate`, generateData);
  }

  // Statistics and Reports
  getShiftStatistics(
    startDate?: Date,
    endDate?: Date,
    employeeCode?: string
  ): Observable<ApiResponse<any>> {
    let params = new HttpParams();

    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }
    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }

    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`, { params });
  }

  // Export Functions
  exportShiftCalendar(filter: ShiftCalendarFilter): Observable<Blob> {
    let params = new HttpParams();

    if (filter.employeeCode) {
      params = params.set('employeeCode', filter.employeeCode);
    }
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString().split('T')[0]);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString().split('T')[0]);
    }
    if (filter.shiftTemplateId !== undefined) {
      params = params.set('shiftTemplateId', filter.shiftTemplateId.toString());
    }
    if (filter.workLocationId !== undefined) {
      params = params.set('workLocationId', filter.workLocationId.toString());
    }

    return this.http.get(`${this.API_URL}/export`, {
      params,
      responseType: 'blob',
      headers: { 'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }
    });
  }

  // Validation and Helper Methods
  validateShiftTime(startTime: string, endTime: string): boolean {
    if (!startTime || !endTime) return false;
    
    const start = this.timeStringToMinutes(startTime);
    const end = this.timeStringToMinutes(endTime);
    
    return end > start;
  }

  calculateShiftDuration(startTime: string, endTime: string): number {
    const start = this.timeStringToMinutes(startTime);
    const end = this.timeStringToMinutes(endTime);
    
    return (end - start) / 60; // Return in hours
  }

  private timeStringToMinutes(timeStr: string): number {
    const [hours, minutes] = timeStr.split(':').map(Number);
    return hours * 60 + minutes;
  }

  formatTime(timeStr: string): string {
    if (!timeStr) return '';
    
    const [hours, minutes] = timeStr.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    
    return `${displayHour}:${minutes} ${ampm}`;
  }

  getDaysOfWeekFromPattern(weekDays: string): string[] {
    if (!weekDays) return [];
    
    const dayNumbers = weekDays.split(',').map(d => parseInt(d.trim()));
    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    
    return dayNumbers.map(num => dayNames[num === 7 ? 0 : num]);
  }

  getRecurrencePatternOptions(): { value: string; label: string }[] {
    return [
      { value: 'DAILY', label: 'Daily' },
      { value: 'WEEKLY', label: 'Weekly' },
      { value: 'MONTHLY', label: 'Monthly' },
      { value: 'CUSTOM', label: 'Custom Days' }
    ];
  }

  getShiftStatusOptions(): { value: string; label: string; color: string }[] {
    return [
      { value: 'SCHEDULED', label: 'Scheduled', color: 'primary' },
      { value: 'ACTIVE', label: 'Active', color: 'success' },
      { value: 'COMPLETED', label: 'Completed', color: 'info' },
      { value: 'CANCELLED', label: 'Cancelled', color: 'danger' },
      { value: 'MODIFIED', label: 'Modified', color: 'warning' }
    ];
  }
}
