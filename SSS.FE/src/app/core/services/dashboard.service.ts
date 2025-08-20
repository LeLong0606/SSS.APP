import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { Employee } from '../models/employee.model';
import { Department } from '../models/department.model';
import { WorkShift } from '../models/work-shift.model';

export interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  totalDepartments: number;
  totalWorkLocations: number;
  todayShifts: number;
  upcomingShifts: number;
  completedShifts: number;
  pendingLeaveRequests: number;
  totalWorkHours: number;
  averageWorkHours: number;
  attendanceRate: number;
}

export interface DashboardAttendanceStatus {
  status: 'CHECKED_IN' | 'CHECKED_OUT' | 'NOT_CHECKED';
  today: string;
  lastCheckIn?: Date;
  lastCheckOut?: Date;
  totalWorkedHours: number;
  currentShift?: WorkShift;
}

export interface RecentActivity {
  recentEmployees: Employee[];
  recentShifts: WorkShift[];
  recentDepartments: Department[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Get comprehensive dashboard statistics
  getDashboardStats(): Observable<DashboardStats> {
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    
    // Create multiple API calls for comprehensive stats
    const calls = {
      employees: this.http.get<PagedResponse<Employee>>(`${this.API_URL}/employee?pageNumber=1&pageSize=1`),
      departments: this.http.get<PagedResponse<Department>>(`${this.API_URL}/department?pageNumber=1&pageSize=1`),
      todayShifts: this.http.get<PagedResponse<WorkShift>>(`${this.API_URL}/workshift?pageNumber=1&pageSize=1&fromDate=${todayStr}&toDate=${todayStr}`),
      employeeStats: this.http.get<ApiResponse<any>>(`${this.API_URL}/employee/statistics`),
      workShiftStats: this.http.get<ApiResponse<any>>(`${this.API_URL}/workshift/statistics`)
    };

    return forkJoin(calls).pipe(
      map((response) => {
        const stats: DashboardStats = {
          totalEmployees: response.employees.totalCount || 0,
          activeEmployees: response.employeeStats.data?.activeEmployees || 0,
          totalDepartments: response.departments.totalCount || 0,
          totalWorkLocations: response.workShiftStats.data?.totalWorkLocations || 0,
          todayShifts: response.todayShifts.totalCount || 0,
          upcomingShifts: response.workShiftStats.data?.upcomingShifts || 0,
          completedShifts: response.workShiftStats.data?.completedShifts || 0,
          pendingLeaveRequests: response.workShiftStats.data?.pendingLeaveRequests || 0,
          totalWorkHours: response.workShiftStats.data?.totalWorkHours || 0,
          averageWorkHours: response.workShiftStats.data?.averageWorkHours || 0,
          attendanceRate: response.workShiftStats.data?.attendanceRate || 0
        };
        return stats;
      }),
      catchError(error => {
        console.error('Error fetching dashboard stats:', error);
        // Return default stats on error
        return of({
          totalEmployees: 0,
          activeEmployees: 0,
          totalDepartments: 0,
          totalWorkLocations: 0,
          todayShifts: 0,
          upcomingShifts: 0,
          completedShifts: 0,
          pendingLeaveRequests: 0,
          totalWorkHours: 0,
          averageWorkHours: 0,
          attendanceRate: 0
        });
      })
    );
  }

  // Get recent activities
  getRecentActivities(): Observable<RecentActivity> {
    const calls = {
      recentEmployees: this.http.get<PagedResponse<Employee>>(`${this.API_URL}/employee?pageNumber=1&pageSize=5&sortBy=createdDate&sortOrder=desc`),
      recentShifts: this.http.get<PagedResponse<WorkShift>>(`${this.API_URL}/workshift?pageNumber=1&pageSize=5&sortBy=shiftDate&sortOrder=desc`),
      recentDepartments: this.http.get<PagedResponse<Department>>(`${this.API_URL}/department?pageNumber=1&pageSize=5&sortBy=createdDate&sortOrder=desc`)
    };

    return forkJoin(calls).pipe(
      map((response) => ({
        recentEmployees: response.recentEmployees.data || [],
        recentShifts: response.recentShifts.data || [],
        recentDepartments: response.recentDepartments.data || []
      })),
      catchError(error => {
        console.error('Error fetching recent activities:', error);
        return of({
          recentEmployees: [],
          recentShifts: [],
          recentDepartments: []
        });
      })
    );
  }

  // Get attendance status for current user
  getAttendanceStatus(employeeCode: string): Observable<DashboardAttendanceStatus> {
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    
    return this.http.get<ApiResponse<WorkShift[]>>(`${this.API_URL}/workshift?employeeCode=${employeeCode}&fromDate=${todayStr}&toDate=${todayStr}&pageSize=1`).pipe(
      map(response => {
        const todayShift = response.data?.[0];
        const status: DashboardAttendanceStatus = {
          status: 'NOT_CHECKED' as const,
          today: today.toLocaleDateString('vi-VN'),
          totalWorkedHours: 0,
          currentShift: todayShift
        };

        if (todayShift) {
          // Logic to determine attendance status based on shift data
          const now = new Date();
          const shiftStart = new Date(`${todayStr}T${todayShift.startTime}`);
          const shiftEnd = new Date(`${todayStr}T${todayShift.endTime}`);
          
          if (now >= shiftStart && now <= shiftEnd) {
            status.status = 'CHECKED_IN';
          } else if (now > shiftEnd) {
            status.status = 'CHECKED_OUT';
          }
        }

        return status;
      }),
      catchError(error => {
        console.error('Error fetching attendance status:', error);
        const defaultStatus: DashboardAttendanceStatus = {
          status: 'NOT_CHECKED',
          today: today.toLocaleDateString('vi-VN'),
          totalWorkedHours: 0
        };
        return of(defaultStatus);
      })
    );
  }

  // Get chart data for analytics
  getChartData(): Observable<any> {
    const today = new Date();
    const lastWeek = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);
    const lastWeekStr = lastWeek.toISOString().split('T')[0];
    const todayStr = today.toISOString().split('T')[0];

    return this.http.get<ApiResponse<any>>(`${this.API_URL}/workshift/analytics?fromDate=${lastWeekStr}&toDate=${todayStr}`).pipe(
      map(response => response.data || {}),
      catchError(error => {
        console.error('Error fetching chart data:', error);
        return of({});
      })
    );
  }

  // Check in/out attendance
  checkIn(employeeCode: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/attendance/checkin`, { employeeCode });
  }

  checkOut(employeeCode: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.API_URL}/attendance/checkout`, { employeeCode });
  }
}
