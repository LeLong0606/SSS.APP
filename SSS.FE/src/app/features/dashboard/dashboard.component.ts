import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, takeUntil, BehaviorSubject, Observable, filter } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserInfo, UserRole } from '../../core/models/auth.model';
import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { WorkShiftService } from '../../core/services/work-shift.service';

interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  totalDepartments: number;
  totalWorkLocations: number;
  todayShifts: number;
  upcomingShifts: number;
  completedShifts: number;
  pendingLeaveRequests?: number;
}

interface AttendanceStatus {
  status: 'CHECKED_IN' | 'CHECKED_OUT' | 'NOT_CHECKED';
  today: string;
  lastCheckIn?: Date;
  lastCheckOut?: Date;
  totalWorkedHours: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  private destroy$ = new Subject<void>();
  private loadingSubject = new BehaviorSubject<boolean>(false);

  currentUser: UserInfo | null = null;
  dashboardStats: DashboardStats = {
    totalEmployees: 0,
    activeEmployees: 0,
    totalDepartments: 0,
    totalWorkLocations: 0,
    todayShifts: 0,
    upcomingShifts: 0,
    completedShifts: 0,
    pendingLeaveRequests: 0
  };

  attendanceStatus: AttendanceStatus = {
    status: 'NOT_CHECKED',
    today: new Date().toLocaleDateString('vi-VN'),
    totalWorkedHours: 0
  };

  isLoading = false;
  loading$ = this.loadingSubject.asObservable();
  
  // Data arrays
  employees: any[] = [];
  workShifts: any[] = [];
  departments: any[] = [];

  // Forms
  employeeForm: FormGroup;
  workShiftForm: FormGroup;
  leaveRequestForm: FormGroup;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router,
    private formBuilder: FormBuilder,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private workShiftService: WorkShiftService
  ) {
    // Initialize forms
    this.employeeForm = this.formBuilder.group({
      employeeCode: ['', Validators.required],
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      departmentId: ['', Validators.required]
    });

    this.workShiftForm = this.formBuilder.group({
      employeeCode: ['', Validators.required],
      shiftDate: ['', Validators.required],
      startTime: ['', Validators.required],
      endTime: ['', Validators.required],
      workLocationId: ['']
    });

    this.leaveRequestForm = this.formBuilder.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['', Validators.required],
      leaveType: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.initializeUser();
    this.loadDashboardData();
    
    // Listen for route changes to refresh dashboard when navigating back to it
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntil(this.destroy$)
    ).subscribe((event) => {
      if (event.url === '/dashboard') {
        console.log('Dashboard reloaded via navigation');
        this.refreshDashboard();
      }
    });
  }

  ngAfterViewInit(): void {
    // Additional initialization after view is ready
    setTimeout(() => {
      this.refreshDashboard();
    }, 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeUser(): void {
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
      });

    if (!this.currentUser) {
      this.authService.getCurrentUser()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success && response.user) {
              this.currentUser = response.user;
            }
          },
          error: (error) => {
            console.error('Error loading current user:', error);
          }
        });
    }
  }

  private loadDashboardData(): void {
    this.loadingSubject.next(true);
    this.isLoading = true;

    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];

    // Fetch counts from backend
    const employeesSub = this.employeeService.getEmployees({ pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        this.dashboardStats.totalEmployees = res.totalCount || 0;
        // Optionally fetch active employees via stats endpoint
        this.employeeService.getEmployeeStats().subscribe({
          next: (stats) => {
            const data = stats.data || {} as any;
            if (typeof data.activeEmployees === 'number') {
              this.dashboardStats.activeEmployees = data.activeEmployees;
            }
          },
          error: () => {}
        });
      },
      error: () => {}
    });

    const departmentsSub = this.departmentService.getDepartments({ pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        this.dashboardStats.totalDepartments = res.totalCount || 0;
      },
      error: () => {}
    });

    const shiftsSub = this.workShiftService.getWorkShifts(1, 1, undefined, todayStr, todayStr).subscribe({
      next: (res) => {
        this.dashboardStats.todayShifts = res.totalCount || 0;
      },
      error: () => {}
    });

    // You can add pendingLeaveRequests when backend endpoint is ready

    // Stop loading after small delay for UX
    setTimeout(() => {
      this.loadingSubject.next(false);
      this.isLoading = false;
    }, 500);
  }

  // Authentication methods
  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  hasRole(role: string): boolean {
    return this.authService.hasRole(role as UserRole);
  }

  // User methods
  getEmployeePhotoUrl(employeeCode?: string): string {
    // Use static placeholder instead of dynamic loading to prevent jerking
    if (!employeeCode) return 'assets/images/default-avatar.svg';
    // For now, use default avatar for all users to prevent loading issues
    // In production, implement proper image caching and lazy loading
    return 'assets/images/default-avatar.svg';
  }

  onImageError(event: any): void {
    // Fallback to default avatar on error
    event.target.src = 'assets/images/default-avatar.svg';
    // Prevent further error events
    event.target.onerror = null;
  }

  // Attendance methods
  checkIn(): void {
    this.attendanceStatus.status = 'CHECKED_IN';
    this.attendanceStatus.lastCheckIn = new Date();
    this.notificationService.showSuccess('Đã check-in thành công');
  }

  checkOut(): void {
    this.attendanceStatus.status = 'CHECKED_OUT';
    this.attendanceStatus.lastCheckOut = new Date();
    this.notificationService.showSuccess('Đã check-out thành công');
  }

  // Form methods
  createEmployee(): void {
    if (this.employeeForm.valid) {
      this.notificationService.showSuccess('Tạo nhân viên thành công');
      this.employeeForm.reset();
    }
  }

  createWorkShift(): void {
    if (this.workShiftForm.valid) {
      this.notificationService.showSuccess('Tạo ca làm việc thành công');
      this.workShiftForm.reset();
    }
  }

  createLeaveRequest(): void {
    if (this.leaveRequestForm.valid) {
      this.notificationService.showSuccess('Tạo đơn xin nghỉ thành công');
      this.leaveRequestForm.reset();
    }
  }

  // File upload methods
  onEmployeePhotoChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.notificationService.showSuccess('Đã tải lên ảnh đại diện');
    }
  }

  onFileSelected(event: any, fileType: string): void {
    const file = event.target.files[0];
    if (file) {
      this.notificationService.showSuccess(`Đã tải lên file ${fileType}`);
    }
  }

  // Utility methods
  formatTime(time: string): string {
    if (!time) return '';
    return time.substring(0, 5); // HH:mm format
  }

  refreshDashboard(): void {
    this.loadDashboardData();
  }

  formatWorkedHours(hours: number): string {
    const wholeHours = Math.floor(hours);
    const minutes = Math.round((hours - wholeHours) * 60);
    return `${wholeHours}h ${minutes}m`;
  }
}
