import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, takeUntil, BehaviorSubject, Observable, filter, interval } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserInfo, UserRole } from '../../core/models/auth.model';
import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { WorkShiftService } from '../../core/services/work-shift.service';
import { DashboardService, DashboardStats, DashboardAttendanceStatus, RecentActivity } from '../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  private destroy$ = new Subject<void>();
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private refreshInterval$ = interval(30000); // Refresh every 30 seconds

  currentUser: UserInfo | null = null;
  dashboardStats: DashboardStats = {
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
  };

  attendanceStatus: DashboardAttendanceStatus = {
    status: 'NOT_CHECKED',
    today: new Date().toLocaleDateString('vi-VN'),
    totalWorkedHours: 0
  };

  recentActivity: RecentActivity = {
    recentEmployees: [],
    recentShifts: [],
    recentDepartments: []
  };

  isLoading = false;
  loading$ = this.loadingSubject.asObservable();
  
  // Data arrays
  employees: any[] = [];
  workShifts: any[] = [];
  departments: any[] = [];

  // Forms
  employeeForm!: FormGroup;
  workShiftForm!: FormGroup;
  leaveRequestForm!: FormGroup;

  // Chart data
  chartData: any = {};

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router,
    private formBuilder: FormBuilder,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private workShiftService: WorkShiftService,
    private dashboardService: DashboardService
  ) {
    this.initializeForms();
  }

  private initializeForms(): void {
    this.employeeForm = this.formBuilder.group({
      employeeCode: ['', Validators.required],
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      position: ['', Validators.required],
      departmentId: ['', Validators.required]
    });

    this.workShiftForm = this.formBuilder.group({
      employeeCode: ['', Validators.required],
      shiftDate: ['', Validators.required],
      startTime: ['', Validators.required],
      endTime: ['', Validators.required],
      workLocationId: ['', Validators.required]
    });

    this.leaveRequestForm = this.formBuilder.group({
      leaveType: ['ANNUAL_LEAVE', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.initializeUser();
    this.loadDashboardData();
    this.setupAutoRefresh();
    
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

  private setupAutoRefresh(): void {
    this.refreshInterval$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.loadDashboardData();
    });
  }

  private initializeUser(): void {
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
        if (this.currentUser?.employeeCode) {
          this.loadAttendanceStatus();
        }
      });

    if (!this.currentUser) {
      this.authService.getCurrentUser()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success && response.user) {
              this.currentUser = response.user;
              if (this.currentUser?.employeeCode) {
                this.loadAttendanceStatus();
              }
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

    // Load dashboard statistics
    this.dashboardService.getDashboardStats()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.dashboardStats = stats;
        },
        error: (error) => {
          console.error('Error loading dashboard stats:', error);
          this.notificationService.showError('Không thể tải thống kê dashboard');
        }
      });

    // Load recent activities
    this.dashboardService.getRecentActivities()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (activity) => {
          this.recentActivity = activity;
          this.employees = activity.recentEmployees;
          this.workShifts = activity.recentShifts;
          this.departments = activity.recentDepartments;
        },
        error: (error) => {
          console.error('Error loading recent activities:', error);
        }
      });

    // Load chart data
    this.dashboardService.getChartData()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.chartData = data;
        },
        error: (error) => {
          console.error('Error loading chart data:', error);
        }
      });

    // Load additional data
    this.loadAdditionalData();

    // Stop loading after small delay for UX
    setTimeout(() => {
      this.loadingSubject.next(false);
      this.isLoading = false;
    }, 500);
  }

  private loadAdditionalData(): void {
    // Load departments for forms
    this.departmentService.getAllDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.departments = response.data || [];
        },
        error: (error) => {
          console.error('Error loading departments:', error);
        }
      });

    // Load employees for forms
    this.employeeService.getEmployees({ pageNumber: 1, pageSize: 50 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.employees = response.data || [];
        },
        error: (error) => {
          console.error('Error loading employees:', error);
        }
      });
  }

  private loadAttendanceStatus(): void {
    if (!this.currentUser?.employeeCode) return;

    this.dashboardService.getAttendanceStatus(this.currentUser.employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (status) => {
          this.attendanceStatus = status;
        },
        error: (error) => {
          console.error('Error loading attendance status:', error);
        }
      });
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
    if (!employeeCode) return 'assets/images/default-avatar.svg';
    return 'assets/images/default-avatar.svg';
  }

  onImageError(event: any): void {
    event.target.src = 'assets/images/default-avatar.svg';
    event.target.onerror = null;
  }

  // Attendance methods
  checkIn(): void {
    if (!this.currentUser?.employeeCode) {
      this.notificationService.showError('Không thể xác định mã nhân viên');
      return;
    }

    this.dashboardService.checkIn(this.currentUser.employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.attendanceStatus.status = 'CHECKED_IN';
            this.attendanceStatus.lastCheckIn = new Date();
            this.notificationService.showSuccess('Đã check-in thành công');
            this.loadAttendanceStatus(); // Refresh status
          } else {
            this.notificationService.showError(response.message || 'Check-in thất bại');
          }
        },
        error: (error) => {
          console.error('Check-in error:', error);
          this.notificationService.showError('Check-in thất bại. Vui lòng thử lại.');
        }
      });
  }

  checkOut(): void {
    if (!this.currentUser?.employeeCode) {
      this.notificationService.showError('Không thể xác định mã nhân viên');
      return;
    }

    this.dashboardService.checkOut(this.currentUser.employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.attendanceStatus.status = 'CHECKED_OUT';
            this.attendanceStatus.lastCheckOut = new Date();
            this.notificationService.showSuccess('Đã check-out thành công');
            this.loadAttendanceStatus(); // Refresh status
          } else {
            this.notificationService.showError(response.message || 'Check-out thất bại');
          }
        },
        error: (error) => {
          console.error('Check-out error:', error);
          this.notificationService.showError('Check-out thất bại. Vui lòng thử lại.');
        }
      });
  }

  // Form methods
  createEmployee(): void {
    if (this.employeeForm.valid) {
      const employeeData = this.employeeForm.value;
      
      this.employeeService.createEmployee(employeeData)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Tạo nhân viên thành công');
              this.employeeForm.reset();
              this.loadDashboardData(); // Refresh data
            } else {
              this.notificationService.showError(response.message || 'Tạo nhân viên thất bại');
            }
          },
          error: (error) => {
            console.error('Create employee error:', error);
            this.notificationService.showError('Tạo nhân viên thất bại. Vui lòng thử lại.');
          }
        });
    }
  }

  createWorkShift(): void {
    if (this.workShiftForm.valid) {
      const shiftData = this.workShiftForm.value;
      
      this.workShiftService.createWorkShift(shiftData)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Tạo ca làm việc thành công');
              this.workShiftForm.reset();
              this.loadDashboardData(); // Refresh data
            } else {
              this.notificationService.showError(response.message || 'Tạo ca làm việc thất bại');
            }
          },
          error: (error) => {
            console.error('Create work shift error:', error);
            this.notificationService.showError('Tạo ca làm việc thất bại. Vui lòng thử lại.');
          }
        });
    }
  }

  createLeaveRequest(): void {
    if (this.leaveRequestForm.valid) {
      // TODO: Implement leave request service
      this.notificationService.showSuccess('Gửi yêu cầu nghỉ phép thành công');
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

  formatPercentage(value: number): string {
    return `${Math.round(value * 100) / 100}%`;
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'CHECKED_IN': return 'bg-success';
      case 'CHECKED_OUT': return 'bg-secondary';
      default: return 'bg-warning';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'CHECKED_IN': return 'Đã vào ca';
      case 'CHECKED_OUT': return 'Đã ra ca';
      default: return 'Chưa chấm công';
    }
  }
}
