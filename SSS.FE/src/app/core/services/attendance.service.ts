import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

// Attendance Models matching backend DTOs
export interface CheckInRequest {
  checkInTime: Date;
  workLocationId?: number;
  notes?: string;
  latitude?: number;
  longitude?: number;
}

export interface CheckOutRequest {
  checkOutTime: Date;
  notes?: string;
  latitude?: number;
  longitude?: number;
}

export interface AttendanceStatus {
  employeeCode: string;
  status: 'CHECKED_IN' | 'CHECKED_OUT' | 'NOT_STARTED';
  today: string;
  lastCheckIn?: Date;
  lastCheckOut?: Date;
  totalWorkedHours: number;
  expectedCheckOut?: Date;
}

export interface AttendanceEventDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  shiftCalendarId?: number;
  eventDateTime: Date;
  eventType: string;
  workLocationId?: number;
  workLocationName?: string;
  deviceInfo?: string;
  ipAddress?: string;
  latitude?: number;
  longitude?: number;
  notes?: string;
  isManualEntry: boolean;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: Date;
  approvalStatus: string;
  createdAt: Date;
}

export interface AttendanceDailyDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  shiftCalendarId: number;
  attendanceDate: Date;
  
  // Actual times
  checkInTime?: Date;
  checkOutTime?: Date;
  breakStartTime?: Date;
  breakEndTime?: Date;
  
  // Scheduled times
  scheduledCheckIn: Date;
  scheduledCheckOut: Date;
  
  // Calculations
  lateMinutes: number;
  earlyLeaveMinutes: number;
  workedMinutes: number;
  workedHours: number;
  standardHours: number;
  overtimeMinutes: number;
  overtimeHours: number;
  deductedHours: number;
  actualWorkDays: number;
  
  // Status
  attendanceStatus: string;
  isComplete: boolean;
  hasTimeViolation: boolean;
  requiresApproval: boolean;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: Date;
  approvalStatus: string;
  
  createdAt: Date;
  updatedAt?: Date;
  notes?: string;
  systemCalculationLog?: string;
}

export interface ManualAttendanceAdjustmentRequest {
  attendanceDailyId: number;
  checkInTime?: Date;
  checkOutTime?: Date;
  reason: string;
  notes?: string;
}

export interface AttendanceDashboardDto {
  date: Date;
  totalEmployees: number;
  presentEmployees: number;
  absentEmployees: number;
  lateEmployees: number;
  earlyLeaveEmployees: number;
  averageWorkHours: number;
  totalOvertimeHours: number;
  recentAttendance: AttendanceDailyDto[];
}

export interface AttendanceReportFilter {
  startDate?: Date;
  endDate?: Date;
  employeeCode?: string;
  departmentId?: number;
  attendanceStatus?: string;
  hasViolations?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private readonly API_URL = `${environment.apiUrl}/attendance`;

  constructor(private http: HttpClient) {}

  // Employee Self-Service Attendance
  checkIn(request: CheckInRequest): Observable<ApiResponse<any>> {
    const checkInData = {
      checkInTime: request.checkInTime.toISOString(),
      workLocationId: request.workLocationId,
      notes: request.notes,
      latitude: request.latitude,
      longitude: request.longitude
    };

    return this.http.post<ApiResponse<any>>(`${this.API_URL}/check-in`, checkInData);
  }

  checkOut(request: CheckOutRequest): Observable<ApiResponse<any>> {
    const checkOutData = {
      checkOutTime: request.checkOutTime.toISOString(),
      notes: request.notes,
      latitude: request.latitude,
      longitude: request.longitude
    };

    return this.http.post<ApiResponse<any>>(`${this.API_URL}/check-out`, checkOutData);
  }

  getCurrentStatus(): Observable<ApiResponse<AttendanceStatus>> {
    return this.http.get<ApiResponse<AttendanceStatus>>(`${this.API_URL}/current-status`);
  }

  // Attendance Events
  getMyAttendanceEvents(
    startDate?: Date,
    endDate?: Date,
    eventType?: string
  ): Observable<ApiResponse<AttendanceEventDto[]>> {
    let params = new HttpParams();

    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }
    if (eventType) {
      params = params.set('eventType', eventType);
    }

    return this.http.get<ApiResponse<AttendanceEventDto[]>>(`${this.API_URL}/my-events`, { params });
  }

  // Manager Functions
  getEmployeeAttendanceEvents(
    employeeCode: string,
    startDate?: Date,
    endDate?: Date,
    eventType?: string
  ): Observable<ApiResponse<AttendanceEventDto[]>> {
    let params = new HttpParams();

    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }
    if (eventType) {
      params = params.set('eventType', eventType);
    }

    return this.http.get<ApiResponse<AttendanceEventDto[]>>(`${this.API_URL}/employee/${employeeCode}/events`, { params });
  }

  approveAttendanceEvent(eventId: number, approvalData: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/events/${eventId}/approve`, approvalData);
  }

  rejectAttendanceEvent(eventId: number, rejectionData: any): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/events/${eventId}/reject`, rejectionData);
  }

  // Daily Attendance Summary
  getMyDailyAttendance(
    startDate?: Date,
    endDate?: Date
  ): Observable<ApiResponse<AttendanceDailyDto[]>> {
    let params = new HttpParams();

    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }

    return this.http.get<ApiResponse<AttendanceDailyDto[]>>(`${this.API_URL}/my-daily`, { params });
  }

  getEmployeeDailyAttendance(
    employeeCode: string,
    startDate?: Date,
    endDate?: Date
  ): Observable<ApiResponse<AttendanceDailyDto[]>> {
    let params = new HttpParams();

    if (startDate) {
      params = params.set('startDate', startDate.toISOString().split('T')[0]);
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString().split('T')[0]);
    }

    return this.http.get<ApiResponse<AttendanceDailyDto[]>>(`${this.API_URL}/employee/${employeeCode}/daily`, { params });
  }

  // Manual Adjustments (Manager/Admin)
  adjustAttendance(request: ManualAttendanceAdjustmentRequest): Observable<ApiResponse<any>> {
    const adjustmentData = {
      attendanceDailyId: request.attendanceDailyId,
      checkInTime: request.checkInTime?.toISOString(),
      checkOutTime: request.checkOutTime?.toISOString(),
      reason: request.reason,
      notes: request.notes
    };

    return this.http.post<ApiResponse<any>>(`${this.API_URL}/adjust`, adjustmentData);
  }

  // Dashboard & Reports
  getDashboard(date?: Date): Observable<ApiResponse<AttendanceDashboardDto>> {
    let params = new HttpParams();

    if (date) {
      params = params.set('date', date.toISOString().split('T')[0]);
    }

    return this.http.get<ApiResponse<AttendanceDashboardDto>>(`${this.API_URL}/dashboard`, { params });
  }

  getAttendanceReport(filter: AttendanceReportFilter): Observable<ApiResponse<AttendanceDailyDto[]>> {
    let params = new HttpParams();

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString().split('T')[0]);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString().split('T')[0]);
    }
    if (filter.employeeCode) {
      params = params.set('employeeCode', filter.employeeCode);
    }
    if (filter.departmentId !== undefined) {
      params = params.set('departmentId', filter.departmentId.toString());
    }
    if (filter.attendanceStatus) {
      params = params.set('attendanceStatus', filter.attendanceStatus);
    }
    if (filter.hasViolations !== undefined) {
      params = params.set('hasViolations', filter.hasViolations.toString());
    }
    if (filter.pageNumber) {
      params = params.set('pageNumber', filter.pageNumber.toString());
    }
    if (filter.pageSize) {
      params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<ApiResponse<AttendanceDailyDto[]>>(`${this.API_URL}/report`, { params });
  }

  // Export Functions
  exportAttendanceReport(filter: AttendanceReportFilter): Observable<Blob> {
    let params = new HttpParams();

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString().split('T')[0]);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString().split('T')[0]);
    }
    if (filter.employeeCode) {
      params = params.set('employeeCode', filter.employeeCode);
    }
    if (filter.departmentId !== undefined) {
      params = params.set('departmentId', filter.departmentId.toString());
    }

    return this.http.get(`${this.API_URL}/export`, {
      params,
      responseType: 'blob',
      headers: { 'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }
    });
  }

  // Helper Methods
  getCurrentDateTime(): Date {
    return new Date();
  }

  isWithinWorkingHours(checkTime: Date, startTime: string, endTime: string): boolean {
    const checkTimeStr = checkTime.toTimeString().substring(0, 5);
    return checkTimeStr >= startTime && checkTimeStr <= endTime;
  }

  calculateWorkedHours(checkIn: Date, checkOut: Date): number {
    const diffMs = checkOut.getTime() - checkIn.getTime();
    return Math.round((diffMs / (1000 * 60 * 60)) * 100) / 100; // Round to 2 decimal places
  }

  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    
    if (hours === 0) {
      return `${remainingMinutes}m`;
    } else if (remainingMinutes === 0) {
      return `${hours}h`;
    } else {
      return `${hours}h ${remainingMinutes}m`;
    }
  }

  getAttendanceStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'present': return 'success';
      case 'absent': return 'danger';
      case 'late': return 'warning';
      case 'partial': return 'info';
      default: return 'secondary';
    }
  }
}
