import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';

// Payroll Models matching backend DTOs
export interface PayrollPeriodDto {
  id: number;
  periodName: string;
  startDate: Date;
  endDate: Date;
  periodType: string;
  status: string;
  lockedAt?: Date;
  lockedBy?: string;
  lockedByName?: string;
  finalizedAt?: Date;
  finalizedBy?: string;
  finalizedByName?: string;
  excelFilePath?: string;
  exportedAt?: Date;
  exportedBy?: string;
  exportedByName?: string;
  createdAt: Date;
  updatedAt?: Date;
  notes?: string;
  totalEmployees: number;
  totalWorkDays: number;
  totalOvertimeHours: number;
}

export interface CreatePayrollPeriodRequest {
  periodName: string;
  startDate: Date;
  endDate: Date;
  periodType: string;
  notes?: string;
}

export interface PayrollSummaryDto {
  id: number;
  payrollPeriodId: number;
  employeeCode: string;
  employeeName: string;
  departmentName?: string;
  totalWorkingDays: number;
  actualWorkDays: number;
  totalWorkHours: number;
  totalOvertimeHours: number;
  totalLateCount: number;
  totalLateMinutes: number;
  totalEarlyLeaveCount: number;
  totalEarlyLeaveMinutes: number;
  annualLeaveDays: number;
  sickLeaveDays: number;
  unpaidLeaveDays: number;
  absentDays: number;
  holidayWorkDays: number;
  deductedHours: number;
  createdAt: Date;
  updatedAt?: Date;
  calculationNotes?: string;
}

export interface LeaveRequestDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  startDate: Date;
  endDate: Date;
  totalDays: number;
  leaveType: string;
  leaveTypeName: string;
  reason: string;
  attachmentPath?: string;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: Date;
  approvalStatus: string;
  approvalNotes?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateLeaveRequestRequest {
  startDate: Date;
  endDate: Date;
  leaveType: string;
  reason: string;
  attachmentFile?: File;
}

export interface ApproveLeaveRequestRequest {
  leaveRequestId: number;
  isApproved: boolean;
  approvalNotes?: string;
}

export interface OvertimeRequestDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  overtimeDate: Date;
  startTime: string;
  endTime: string;
  plannedHours: number;
  reason: string;
  workLocationId?: number;
  workLocationName?: string;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: Date;
  approvalStatus: string;
  approvalNotes?: string;
  actualHours?: number;
  actualStartTime?: Date;
  actualEndTime?: Date;
  createdAt: Date;
}

export interface CreateOvertimeRequestRequest {
  overtimeDate: Date;
  startTime: string;
  endTime: string;
  reason: string;
  workLocationId?: number;
}

export interface ExcelExportRequest {
  payrollPeriodId: number;
  employeeCodes?: string[];
  departmentIds?: number[];
  exportFormat: string;
  includeAttendanceLog: boolean;
  includeViolations: boolean;
}

export interface ExcelExportResult {
  success: boolean;
  message: string;
  filePath?: string;
  fileName?: string;
  fileSize?: number;
  exportedAt: Date;
  exportedBy: string;
  errors: string[];
}

export interface PayrollFilter {
  startDate?: Date;
  endDate?: Date;
  employeeCode?: string;
  departmentId?: number;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PayrollService {
  private readonly API_URL = `${environment.apiUrl}/payroll`;

  constructor(private http: HttpClient) {}

  // Payroll Periods Management
  getPayrollPeriods(
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string,
    year?: number
  ): Observable<PagedResponse<PayrollPeriodDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }
    if (year) {
      params = params.set('year', year.toString());
    }

    return this.http.get<PagedResponse<PayrollPeriodDto>>(`${this.API_URL}/periods`, { params });
  }

  getPayrollPeriod(id: number): Observable<ApiResponse<PayrollPeriodDto>> {
    return this.http.get<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods/${id}`);
  }

  createPayrollPeriod(period: CreatePayrollPeriodRequest): Observable<ApiResponse<PayrollPeriodDto>> {
    const periodData = {
      ...period,
      startDate: period.startDate.toISOString().split('T')[0],
      endDate: period.endDate.toISOString().split('T')[0]
    };

    return this.http.post<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods`, periodData);
  }

  updatePayrollPeriod(id: number, period: CreatePayrollPeriodRequest): Observable<ApiResponse<PayrollPeriodDto>> {
    const periodData = {
      ...period,
      startDate: period.startDate.toISOString().split('T')[0],
      endDate: period.endDate.toISOString().split('T')[0]
    };

    return this.http.put<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods/${id}`, periodData);
  }

  deletePayrollPeriod(id: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.API_URL}/periods/${id}`);
  }

  lockPayrollPeriod(id: number): Observable<ApiResponse<PayrollPeriodDto>> {
    return this.http.post<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods/${id}/lock`, {});
  }

  unlockPayrollPeriod(id: number): Observable<ApiResponse<PayrollPeriodDto>> {
    return this.http.post<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods/${id}/unlock`, {});
  }

  finalizePayrollPeriod(id: number): Observable<ApiResponse<PayrollPeriodDto>> {
    return this.http.post<ApiResponse<PayrollPeriodDto>>(`${this.API_URL}/periods/${id}/finalize`, {});
  }

  // Payroll Summary
  getPayrollSummary(
    payrollPeriodId: number,
    pageNumber: number = 1,
    pageSize: number = 50,
    employeeCode?: string,
    departmentId?: number
  ): Observable<PagedResponse<PayrollSummaryDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }
    if (departmentId !== undefined) {
      params = params.set('departmentId', departmentId.toString());
    }

    return this.http.get<PagedResponse<PayrollSummaryDto>>(`${this.API_URL}/periods/${payrollPeriodId}/summary`, { params });
  }

  getEmployeePayrollSummary(
    payrollPeriodId: number,
    employeeCode: string
  ): Observable<ApiResponse<PayrollSummaryDto>> {
    return this.http.get<ApiResponse<PayrollSummaryDto>>(`${this.API_URL}/periods/${payrollPeriodId}/summary/${employeeCode}`);
  }

  recalculatePayrollSummary(payrollPeriodId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/periods/${payrollPeriodId}/recalculate`, {});
  }

  // Leave Requests
  createLeaveRequest(request: CreateLeaveRequestRequest): Observable<ApiResponse<LeaveRequestDto>> {
    const formData = new FormData();
    formData.append('startDate', request.startDate.toISOString().split('T')[0]);
    formData.append('endDate', request.endDate.toISOString().split('T')[0]);
    formData.append('leaveType', request.leaveType);
    formData.append('reason', request.reason);
    
    if (request.attachmentFile) {
      formData.append('attachmentFile', request.attachmentFile);
    }

    return this.http.post<ApiResponse<LeaveRequestDto>>(`${this.API_URL}/leave-requests`, formData);
  }

  getMyLeaveRequests(
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string
  ): Observable<PagedResponse<LeaveRequestDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PagedResponse<LeaveRequestDto>>(`${this.API_URL}/leave-requests/my`, { params });
  }

  getLeaveRequests(
    pageNumber: number = 1,
    pageSize: number = 20,
    employeeCode?: string,
    status?: string,
    leaveType?: string
  ): Observable<PagedResponse<LeaveRequestDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }
    if (status) {
      params = params.set('status', status);
    }
    if (leaveType) {
      params = params.set('leaveType', leaveType);
    }

    return this.http.get<PagedResponse<LeaveRequestDto>>(`${this.API_URL}/leave-requests`, { params });
  }

  approveLeaveRequest(request: ApproveLeaveRequestRequest): Observable<ApiResponse<LeaveRequestDto>> {
    return this.http.post<ApiResponse<LeaveRequestDto>>(`${this.API_URL}/leave-requests/${request.leaveRequestId}/approve`, {
      isApproved: request.isApproved,
      approvalNotes: request.approvalNotes
    });
  }

  cancelLeaveRequest(leaveRequestId: number, reason: string): Observable<ApiResponse<LeaveRequestDto>> {
    return this.http.post<ApiResponse<LeaveRequestDto>>(`${this.API_URL}/leave-requests/${leaveRequestId}/cancel`, {
      reason: reason
    });
  }

  // Overtime Requests
  createOvertimeRequest(request: CreateOvertimeRequestRequest): Observable<ApiResponse<OvertimeRequestDto>> {
    const overtimeData = {
      ...request,
      overtimeDate: request.overtimeDate.toISOString().split('T')[0]
    };

    return this.http.post<ApiResponse<OvertimeRequestDto>>(`${this.API_URL}/overtime-requests`, overtimeData);
  }

  getMyOvertimeRequests(
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string
  ): Observable<PagedResponse<OvertimeRequestDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PagedResponse<OvertimeRequestDto>>(`${this.API_URL}/overtime-requests/my`, { params });
  }

  getOvertimeRequests(
    pageNumber: number = 1,
    pageSize: number = 20,
    employeeCode?: string,
    status?: string
  ): Observable<PagedResponse<OvertimeRequestDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (employeeCode) {
      params = params.set('employeeCode', employeeCode);
    }
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<PagedResponse<OvertimeRequestDto>>(`${this.API_URL}/overtime-requests`, { params });
  }

  approveOvertimeRequest(
    overtimeRequestId: number,
    isApproved: boolean,
    approvalNotes?: string
  ): Observable<ApiResponse<OvertimeRequestDto>> {
    return this.http.post<ApiResponse<OvertimeRequestDto>>(`${this.API_URL}/overtime-requests/${overtimeRequestId}/approve`, {
      isApproved: isApproved,
      approvalNotes: approvalNotes
    });
  }

  // Excel Export
  exportPayrollToExcel(request: ExcelExportRequest): Observable<ApiResponse<ExcelExportResult>> {
    return this.http.post<ApiResponse<ExcelExportResult>>(`${this.API_URL}/export/excel`, request);
  }

  downloadExportFile(filePath: string): Observable<Blob> {
    const params = new HttpParams().set('filePath', filePath);
    
    return this.http.get(`${this.API_URL}/export/download`, {
      params,
      responseType: 'blob'
    });
  }

  // Reports and Statistics
  getPayrollStatistics(
    payrollPeriodId?: number,
    departmentId?: number
  ): Observable<ApiResponse<any>> {
    let params = new HttpParams();

    if (payrollPeriodId !== undefined) {
      params = params.set('payrollPeriodId', payrollPeriodId.toString());
    }
    if (departmentId !== undefined) {
      params = params.set('departmentId', departmentId.toString());
    }

    return this.http.get<ApiResponse<any>>(`${this.API_URL}/statistics`, { params });
  }

  // Helper Methods
  getLeaveTypeOptions(): { value: string; label: string; color: string }[] {
    return [
      { value: 'ANNUAL_LEAVE', label: 'Annual Leave', color: 'primary' },
      { value: 'SICK_LEAVE', label: 'Sick Leave', color: 'warning' },
      { value: 'MATERNITY', label: 'Maternity Leave', color: 'info' },
      { value: 'PATERNITY', label: 'Paternity Leave', color: 'info' },
      { value: 'UNPAID', label: 'Unpaid Leave', color: 'secondary' },
      { value: 'EMERGENCY', label: 'Emergency Leave', color: 'danger' }
    ];
  }

  getLeaveStatusOptions(): { value: string; label: string; color: string }[] {
    return [
      { value: 'PENDING', label: 'Pending', color: 'warning' },
      { value: 'APPROVED', label: 'Approved', color: 'success' },
      { value: 'REJECTED', label: 'Rejected', color: 'danger' },
      { value: 'CANCELLED', label: 'Cancelled', color: 'secondary' }
    ];
  }

  getPeriodTypeOptions(): { value: string; label: string }[] {
    return [
      { value: 'WEEKLY', label: 'Weekly' },
      { value: 'BIWEEKLY', label: 'Bi-weekly' },
      { value: 'MONTHLY', label: 'Monthly' },
      { value: 'QUARTERLY', label: 'Quarterly' }
    ];
  }

  calculateLeaveDays(startDate: Date, endDate: Date): number {
    const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(amount);
  }

  validateDateRange(startDate: Date, endDate: Date): boolean {
    return startDate <= endDate;
  }

  isWeekend(date: Date): boolean {
    const day = date.getDay();
    return day === 0 || day === 6; // Sunday = 0, Saturday = 6
  }
}
