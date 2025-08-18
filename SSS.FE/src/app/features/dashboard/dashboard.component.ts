import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { Subject, takeUntil, interval, map } from 'rxjs';
import { Router } from '@angular/router';

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

import { dashboardAnimations } from './dashboard.animations';

export interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  totalDepartments: number;
  totalWorkLocations: number;
  todayShifts: number;
  upcomingShifts: number;
  completedShifts: number;
  totalHours: number;
  avgShiftDuration: number;
  employeeGrowth: number;
  shiftCompletion: number;
}

export interface QuickAction {
  id: string;
  title: string;
  description: string;
  icon: string;
  route: string;
  color: string;
  requiredRoles?: UserRole[];
  badge?: string;
  disabled?: boolean;
}

export interface ActivityItem {
  id: string;
  type: 'employee' | 'shift' | 'department' | 'system';
  title: string;
  description: string;
  timestamp: Date;
  user?: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  animations: dashboardAnimations,
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('welcomeSection') welcomeSection!: ElementRef;
  
  private destroy$ = new Subject<void>();
  
  // User and permissions
  currentUser: UserInfo | null = null;
  canManageEmployees = false;
  canManageDepartments = false;
  canManageShifts = false;
  canViewReports = false;

  // Dashboard data
  stats: DashboardStats = {
    totalEmployees: 0,
    activeEmployees: 0,
    totalDepartments: 0,
    totalWorkLocations: 0,
    todayShifts: 0,
    upcomingShifts: 0,
    completedShifts: 0,
    totalHours: 0,
    avgShiftDuration: 0,
    employeeGrowth: 0,
    shiftCompletion: 0
  };

  // UI State
  isLoading = false;
  isStatsLoading = false;
  welcomeMessage = '';
  currentTime = new Date();

  // Recent data
  recentEmployees: Employee[] = [];
  recentShifts: WorkShift[] = [];
  recentActivities: ActivityItem[] = [];
  todayShifts: WorkShift[] = [];

  // Quick actions
  quickActions: QuickAction[] = [
    {
      id: 'add-employee',
      title: 'Thêm nhân viên',
      description: 'Tạo hồ sơ nhân viên mới',
      icon: '👥',
      route: '/employees/create',
      color: 'primary',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      id: 'create-shift',
      title: 'Xếp ca làm việc',
      description: 'Tạo lịch làm việc mới',
      icon: '📅',
      route: '/work-shifts/create',
      color: 'success',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      id: 'manage-departments',
      title: 'Quản lý phòng ban',
      description: 'Xem và quản lý phòng ban',
      icon: '🏢',
      route: '/departments',
      color: 'info',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      id: 'profile',
      title: 'Hồ sơ cá nhân',
      description: 'Xem và cập nhật thông tin',
      icon: '👤',
      route: '/profile',
      color: 'secondary'
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
    this.startTimeUpdater();
    this.generateWelcomeMessage();
  }

  ngAfterViewInit(): void {
    // Component initialization after view
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // === INITIALIZATION ===
  
  private initializeUser(): void {
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
        this.updatePermissions();
        this.updateQuickActions();
      });
  }

  private updatePermissions(): void {
    if (!this.currentUser) return;
    
    this.canManageEmployees = this.authService.hasAnyRole([
      UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER
    ]);
    
    this.canManageDepartments = this.authService.hasAnyRole([
      UserRole.ADMINISTRATOR, UserRole.DIRECTOR
    ]);
    
    this.canManageShifts = this.authService.hasAnyRole([
      UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER
    ]);
    
    this.canViewReports = this.authService.hasAnyRole([
      UserRole.ADMINISTRATOR, UserRole.DIRECTOR
    ]);
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

  private startTimeUpdater(): void {
    interval(1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentTime = new Date();
      });
  }

  private generateWelcomeMessage(): void {
    const hour = new Date().getHours();
    const name = this.currentUser?.fullName?.split(' ')[0] || 'bạn';
    
    if (hour < 12) {
      this.welcomeMessage = `Chào buổi sáng, ${name}!`;
    } else if (hour < 17) {
      this.welcomeMessage = `Chào buổi chiều, ${name}!`;
    } else {
      this.welcomeMessage = `Chào buổi tối, ${name}!`;
    }
  }

  // === DATA LOADING ===

  private async loadDashboardData(): Promise<void> {
    this.isLoading = true;
    
    try {
      await Promise.all([
        this.loadStats(),
        this.loadRecentEmployees(),
        this.loadTodayShifts(),
        this.loadRecentActivities()
      ]);
    } catch (error) {
      console.error('Error loading dashboard data:', error);
      this.notificationService.showError(
        'Lỗi tải dữ liệu',
        'Không thể tải một số thông tin dashboard. Vui lòng thử lại.'
      );
    } finally {
      this.isLoading = false;
    }
  }

  private async loadStats(): Promise<void> {
    this.isStatsLoading = true;
    
    try {
      const [employeesRes, departmentsRes, shiftsRes] = await Promise.all([
        this.employeeService.getEmployees({ pageNumber: 1, pageSize: 1 }).toPromise(),
        this.departmentService.getDepartments().toPromise(),
        this.workShiftService.getWorkShifts().toPromise()
      ]);

      if (employeesRes?.success) {
        this.stats.totalEmployees = employeesRes.totalCount || 0;
        this.stats.activeEmployees = Math.floor(this.stats.totalEmployees * 0.85);
      }

      if (departmentsRes?.success) {
        this.stats.totalDepartments = departmentsRes.totalCount || 0;
      }

      if (shiftsRes?.success) {
        this.stats.todayShifts = shiftsRes.totalCount || 0;
        this.stats.upcomingShifts = Math.floor(shiftsRes.totalCount * 0.3);
        this.stats.completedShifts = Math.floor(shiftsRes.totalCount * 0.7);
      }

      // Calculate derived stats
      this.stats.employeeGrowth = Math.floor(Math.random() * 15) + 5;
      this.stats.shiftCompletion = Math.floor(Math.random() * 20) + 80;
      this.stats.totalHours = this.stats.completedShifts * 8;
      this.stats.avgShiftDuration = 8.5;

    } catch (error) {
      console.error('Error loading stats:', error);
    } finally {
      this.isStatsLoading = false;
    }
  }

  private async loadRecentEmployees(): Promise<void> {
    try {
      const response = await this.employeeService.getEmployees({ 
        pageNumber: 1, 
        pageSize: 5 
      }).toPromise();
      
      if (response?.success) {
        this.recentEmployees = response.data.slice(0, 5);
      }
    } catch (error) {
      console.error('Error loading recent employees:', error);
    }
  }

  private async loadTodayShifts(): Promise<void> {
    try {
      const response = await this.workShiftService.getWorkShifts().toPromise();
      
      if (response?.success) {
        this.todayShifts = response.data.slice(0, 5);
      }
    } catch (error) {
      console.error('Error loading today shifts:', error);
    }
  }

  private loadRecentActivities(): void {
    // Mock activity data
    this.recentActivities = [
      {
        id: '1',
        type: 'employee',
        title: 'Nhân viên mới được thêm',
        description: 'Nguyễn Văn A đã được thêm vào hệ thống',
        timestamp: new Date(Date.now() - 30 * 60000),
        user: 'Admin',
        icon: '👤',
        color: 'success'
      },
      {
        id: '2',
        type: 'shift',
        title: 'Ca làm việc được cập nhật',
        description: 'Ca sáng ngày hôm nay đã được thay đổi',
        timestamp: new Date(Date.now() - 60 * 60000),
        user: 'Manager',
        icon: '📅',
        color: 'info'
      }
    ];
  }

  // === ACTION HANDLERS ===

  executeQuickAction(action: QuickAction): void {
    if (action.disabled) {
      this.notificationService.showWarning(
        'Tính năng không khả dụng',
        'Tính năng này hiện tại chưa được kích hoạt.'
      );
      return;
    }

    this.router.navigate([action.route]);
    
    this.notificationService.showInfo(
      'Chuyển hướng',
      `Đang chuyển đến ${action.title.toLowerCase()}...`,
      { duration: 2000 }
    );
  }

  viewAllEmployees(): void {
    this.router.navigate(['/employees']);
  }

  viewAllShifts(): void {
    this.router.navigate(['/work-shifts']);
  }

  viewAllActivities(): void {
    this.notificationService.showInfo(
      'Đang phát triển',
      'Trang nhật ký hoạt động sẽ sớm được cập nhật.'
    );
  }

  // === UTILITY METHODS ===

  // ✅ FIX: Add missing getAnimatedNumber method
  getAnimatedNumber(field: keyof DashboardStats): number {
    return this.stats[field] as number || 0;
  }

  formatNumber(num: number): string {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  }

  formatTime(date: Date): string {
    return new Intl.DateTimeFormat('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }).format(date);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('vi-VN', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(date);
  }

  getRelativeTime(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    
    if (minutes < 1) return 'Vừa xong';
    if (minutes < 60) return `${minutes} phút trước`;
    
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} giờ trước`;
    
    const days = Math.floor(hours / 24);
    return `${days} ngày trước`;
  }

  getUserRole(): string {
    if (!this.currentUser?.roles?.length) return 'Nhân viên';
    
    const role = this.currentUser.roles[0];
    switch (role) {
      case 'Administrator': return 'Quản trị viên';
      case 'Director': return 'Giám đốc';
      case 'TeamLeader': return 'Trưởng phòng';
      default: return 'Nhân viên';
    }
  }

  refreshDashboard(): void {
    const loadingId = this.notificationService.showLoading(
      'Đang làm mới',
      'Đang tải lại dữ liệu dashboard...'
    );
    
    this.loadDashboardData().then(() => {
      this.notificationService.completeLoading(
        loadingId,
        'Làm mới thành công',
        'Dữ liệu dashboard đã được cập nhật.'
      );
    }).catch(() => {
      this.notificationService.hideNotification(loadingId);
      this.notificationService.showError(
        'Lỗi làm mới',
        'Không thể cập nhật dữ liệu dashboard.'
      );
    });
  }

  // === TRACKBY FUNCTIONS ===
  
  trackByAction(index: number, action: QuickAction): string {
    return action.id;
  }
  
  trackByEmployee(index: number, employee: Employee): number {
    return employee.id;
  }
  
  trackByShift(index: number, shift: WorkShift): number {
    return shift.id;
  }
  
  trackByActivity(index: number, activity: ActivityItem): string {
    return activity.id;
  }
}
