import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { WorkShiftService } from '../../core/services/work-shift.service';
import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';

import { UserInfo, UserRole } from '../../core/models/auth.model';
import { Employee } from '../../core/models/employee.model';
import { Department } from '../../core/models/department.model';
import { WorkShift } from '../../core/models/work-shift.model';
import { WorkLocation } from '../../core/models/work-location.model';

interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  totalDepartments: number;
  totalWorkLocations: number;
  todayShifts: number;
  upcomingShifts: number;
  completedShifts: number;
}

interface QuickAction {
  title: string;
  description: string;
  icon: string;
  route: string;
  requiredRoles?: UserRole[];
  color: string;
}

interface RecentActivity {
  id: string;
  type: 'employee' | 'department' | 'shift' | 'location';
  title: string;
  description: string;
  timestamp: Date;
  icon: string;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  currentUser: UserInfo | null = null;
  dashboardStats: DashboardStats = {
    totalEmployees: 0,
    activeEmployees: 0,
    totalDepartments: 0,
    totalWorkLocations: 0,
    todayShifts: 0,
    upcomingShifts: 0,
    completedShifts: 0
  };

  isLoading = false;
  isStatsLoading = false;

  // Recent data
  recentEmployees: Employee[] = [];
  recentShifts: WorkShift[] = [];
  todayShifts: WorkShift[] = [];
  upcomingShifts: WorkShift[] = [];

  // Quick actions based on user roles
  quickActions: QuickAction[] = [
    {
      title: 'Thêm nhân viên',
      description: 'Tạo hồ sơ nhân viên mới',
      icon: '👥',
      route: '/employees/create',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER],
      color: 'primary'
    },
    {
      title: 'Xếp ca làm việc',
      description: 'Tạo lịch làm việc mới',
      icon: '📅',
      route: '/work-shifts/create',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER],
      color: 'success'
    },
    {
      title: 'Quản lý phòng ban',
      description: 'Xem và quản lý phòng ban',
      icon: '🏢',
      route: '/departments',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER],
      color: 'info'
    },
    {
      title: 'Địa điểm làm việc',
      description: 'Quản lý các địa điểm',
      icon: '📍',
      route: '/work-locations',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR],
      color: 'warning'
    },
    {
      title: 'Hồ sơ cá nhân',
      description: 'Xem và cập nhật thông tin',
      icon: '👤',
      route: '/profile',
      color: 'secondary'
    },
    {
      title: 'Báo cáo',
      description: 'Xem báo cáo và thống kê',
      icon: '📊',
      route: '/reports',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR],
      color: 'purple'
    }
  ];

  filteredQuickActions: QuickAction[] = [];

  constructor(
    private authService: AuthService,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private workShiftService: WorkShiftService,
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeUser();
    this.loadDashboardData();
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
        this.updateQuickActions();
      });

    if (!this.currentUser) {
      this.authService.getCurrentUser()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success && response.user) {
              this.currentUser = response.user;
              this.updateQuickActions();
            }
          },
          error: (error) => {
            console.error('Error loading current user:', error);
          }
        });
    }
  }

  private updateQuickActions(): void {
    if (!this.currentUser) {
      this.filteredQuickActions = [];
      return;
    }

    this.filteredQuickActions = this.quickActions.filter(action => {
      if (!action.requiredRoles || action.requiredRoles.length === 0) {
        return true;
      }
      return this.authService.hasAnyRole(action.requiredRoles);
    });
  }

  private loadDashboardData(): void {
    this.isLoading = true;
    this.loadStatistics();
    this.loadRecentData();
  }

  private loadStatistics(): void {
    this.isStatsLoading = true;

    // Load basic statistics
    forkJoin({
      employees: this.employeeService.getEmployeeStats(),
      departments: this.departmentService.getDepartmentStats(),
      workLocations: this.workLocationService.getWorkLocationStats(),
      workShifts: this.workShiftService.getWorkShiftStats()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (results) => {
          // Process employee stats
          if (results.employees.success && results.employees.data) {
            this.dashboardStats.totalEmployees = results.employees.data.totalEmployees || 0;
            this.dashboardStats.activeEmployees = results.employees.data.activeEmployees || 0;
          }

          // Process department stats  
          if (results.departments.success && results.departments.data) {
            this.dashboardStats.totalDepartments = results.departments.data.totalDepartments || 0;
          }

          // Process work location stats
          if (results.workLocations.success && results.workLocations.data) {
            this.dashboardStats.totalWorkLocations = results.workLocations.data.totalLocations || 0;
          }

          // Process work shift stats
          if (results.workShifts.success && results.workShifts.data) {
            this.dashboardStats.todayShifts = results.workShifts.data.todayShifts || 0;
            this.dashboardStats.upcomingShifts = results.workShifts.data.upcomingShifts || 0;
            this.dashboardStats.completedShifts = results.workShifts.data.completedShifts || 0;
          }

          this.isStatsLoading = false;
        },
        error: (error) => {
          console.error('Error loading dashboard statistics:', error);
          this.isStatsLoading = false;
        }
      });
  }

  private loadRecentData(): void {
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    
    const tomorrow = new Date(today);
    tomorrow.setDate(today.getDate() + 7); // Next 7 days
    const tomorrowStr = tomorrow.toISOString().split('T')[0];

    forkJoin({
      recentEmployees: this.employeeService.getEmployees({ pageNumber: 1, pageSize: 5 }),
      todayShifts: this.workShiftService.getShiftsByDateRange(todayStr, todayStr),
      upcomingShifts: this.workShiftService.getShiftsByDateRange(todayStr, tomorrowStr)
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (results) => {
          // Recent employees
          if (results.recentEmployees.success && results.recentEmployees.data) {
            this.recentEmployees = results.recentEmployees.data.slice(0, 5);
          }

          // Today's shifts
          if (results.todayShifts.success && results.todayShifts.data) {
            this.todayShifts = results.todayShifts.data.slice(0, 10);
          }

          // Upcoming shifts
          if (results.upcomingShifts.success && results.upcomingShifts.data) {
            this.upcomingShifts = results.upcomingShifts.data
              .filter(shift => {
                const shiftDateStr = typeof shift.shiftDate === 'string' 
                  ? shift.shiftDate 
                  : new Date(shift.shiftDate).toISOString().split('T')[0];
                return shiftDateStr > todayStr;
              })
              .slice(0, 10);
          }

          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading recent data:', error);
          this.isLoading = false;
        }
      });
  }

  // User greeting based on time
  getGreeting(): string {
    const hour = new Date().getHours();
    const name = this.currentUser?.fullName || 'bạn';
    
    if (hour < 12) {
      return `Chào buổi sáng, ${name}!`;
    } else if (hour < 18) {
      return `Chào buổi chiều, ${name}!`;
    } else {
      return `Chào buổi tối, ${name}!`;
    }
  }

  getWelcomeMessage(): string {
    const role = this.getUserRole();
    return `Chào mừng ${role} đến với hệ thống quản lý nhân viên SSS`;
  }

  getUserRole(): string {
    if (!this.currentUser || !this.currentUser.roles || this.currentUser.roles.length === 0) {
      return 'Nhân viên';
    }

    const role = this.currentUser.roles[0];
    switch (role) {
      case UserRole.ADMINISTRATOR:
        return 'Quản trị viên';
      case UserRole.DIRECTOR:
        return 'Giám đốc';
      case UserRole.TEAM_LEADER:
        return 'Trưởng phòng';
      case UserRole.EMPLOYEE:
        return 'Nhân viên';
      default:
        return 'Nhân viên';
    }
  }

  getUserDisplayName(): string {
    return this.currentUser?.fullName || 'Người dùng';
  }

  // Navigation methods
  navigateToAction(action: QuickAction): void {
    this.router.navigate([action.route]);
  }

  viewAllEmployees(): void {
    this.router.navigate(['/employees']);
  }

  viewAllShifts(): void {
    this.router.navigate(['/work-shifts']);
  }

  viewAllDepartments(): void {
    this.router.navigate(['/departments']);
  }

  viewAllLocations(): void {
    this.router.navigate(['/work-locations']);
  }

  // Utility methods
  formatDate(date: Date | string): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN');
  }

  formatTime(time: string): string {
    if (!time) return '';
    return time.substring(0, 5); // HH:mm format
  }

  getShiftStatusClass(shift: WorkShift): string {
    const shiftDate = new Date(shift.shiftDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    if (shiftDate.toDateString() === today.toDateString()) {
      return 'shift-today';
    } else if (shiftDate > today) {
      return 'shift-upcoming';
    } else {
      return 'shift-past';
    }
  }

  getEmployeeStatusClass(employee: Employee): string {
    return employee.isActive ? 'employee-active' : 'employee-inactive';
  }

  // Chart data methods (for future chart implementation)
  getEmployeeChartData(): any {
    return {
      labels: ['Đang làm việc', 'Đã nghỉ việc'],
      data: [
        this.dashboardStats.activeEmployees,
        this.dashboardStats.totalEmployees - this.dashboardStats.activeEmployees
      ],
      colors: ['#28a745', '#dc3545']
    };
  }

  getShiftChartData(): any {
    return {
      labels: ['Hôm nay', 'Sắp tới', 'Đã hoàn thành'],
      data: [
        this.dashboardStats.todayShifts,
        this.dashboardStats.upcomingShifts,
        this.dashboardStats.completedShifts
      ],
      colors: ['#007bff', '#ffc107', '#28a745']
    };
  }

  // Refresh data
  refresh(): void {
    this.loadDashboardData();
    this.notificationService.showSuccess('Đã cập nhật dữ liệu dashboard');
  }

  // Quick stats calculations
  getEmployeeGrowthPercentage(): number {
    // TODO: Calculate based on historical data
    return 5.2;
  }

  getShiftCompletionRate(): number {
    const total = this.dashboardStats.completedShifts + this.dashboardStats.todayShifts + this.dashboardStats.upcomingShifts;
    if (total === 0) return 0;
    
    return Math.round((this.dashboardStats.completedShifts / total) * 100);
  }

  getDepartmentUtilization(): number {
    // TODO: Calculate based on employees per department
    const avgEmployeesPerDept = this.dashboardStats.totalDepartments > 0 
      ? this.dashboardStats.activeEmployees / this.dashboardStats.totalDepartments 
      : 0;
    
    // Assume optimal is around 10 employees per department
    return Math.min(100, Math.round((avgEmployeesPerDept / 10) * 100));
  }
}
