import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { Subject, takeUntil, interval, map } from 'rxjs';
import { Router } from '@angular/router';
import { trigger, transition, style, animate, query, stagger, keyframes } from '@angular/animations';

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
  animations: [
    // Card entrance animation
    trigger('cardSlideIn', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(50px) scale(0.8)' }),
          stagger(100, [
            animate('600ms cubic-bezier(0.35, 0, 0.25, 1)', 
              style({ opacity: 1, transform: 'translateY(0) scale(1)' })
            )
          ])
        ], { optional: true })
      ])
    ]),
    
    // Stats counter animation
    trigger('counterAnimation', [
      transition(':enter', [
        animate('1200ms ease-out', keyframes([
          style({ transform: 'scale(0.8)', opacity: 0, offset: 0 }),
          style({ transform: 'scale(1.1)', opacity: 0.8, offset: 0.7 }),
          style({ transform: 'scale(1)', opacity: 1, offset: 1 })
        ]))
      ])
    ]),
    
    // Quick actions hover
    trigger('actionHover', [
      transition('idle => hover', [
        animate('200ms ease-out', 
          style({ transform: 'translateY(-8px) scale(1.02)' })
        )
      ]),
      transition('hover => idle', [
        animate('200ms ease-out', 
          style({ transform: 'translateY(0) scale(1)' })
        )
      ])
    ]),
    
    // Activity list animation
    trigger('listAnimation', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateX(-50px)' }),
          stagger(50, [
            animate('300ms ease-out', 
              style({ opacity: 1, transform: 'translateX(0)' })
            )
          ])
        ], { optional: true })
      ])
    ])
  ],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('welcomeSection') welcomeSection!: ElementRef;
  
  private destroy$ = new Subject<void>();
  private animatedNumbers: { [key: string]: number } = {};
  
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
      id: 'work-locations',
      title: 'Địa điểm làm việc',
      description: 'Quản lý các địa điểm',
      icon: '📍',
      route: '/work-locations',
      color: 'warning',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR]
    },
    {
      id: 'profile',
      title: 'Hồ sơ cá nhân',
      description: 'Xem và cập nhật thông tin',
      icon: '👤',
      route: '/profile',
      color: 'secondary'
    },
    {
      id: 'reports',
      title: 'Báo cáo & Thống kê',
      description: 'Xem báo cáo chi tiết',
      icon: '📊',
      route: '/reports',
      color: 'purple',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR]
    }
  ];

  filteredQuickActions: QuickAction[] = [];

  // Chart data (for future implementation)
  chartData: any = {};
  chartOptions: any = {};

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
    // Initialize AOS (Animate On Scroll) if available
    if (typeof (window as any).AOS !== 'undefined') {
      (window as any).AOS.init({
        duration: 800,
        once: true,
        offset: 100
      });
    }
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
      
      this.animateNumbers();
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
      // Load basic counts
      const [employeesRes, departmentsRes, locationsRes, shiftsRes] = await Promise.all([
        this.employeeService.getEmployees({ pageNumber: 1, pageSize: 1 }).toPromise(),
        this.departmentService.getDepartments(1, 1).toPromise(),
        this.workLocationService.getWorkLocations(1, 1).toPromise(),
        this.workShiftService.getWorkShifts(1, 1).toPromise()
      ]);

      if (employeesRes?.success) {
        this.stats.totalEmployees = employeesRes.totalCount || 0;
        this.stats.activeEmployees = Math.floor(this.stats.totalEmployees * 0.85); // Estimate
      }

      if (departmentsRes?.success) {
        this.stats.totalDepartments = departmentsRes.totalCount || 0;
      }

      if (locationsRes?.success) {
        this.stats.totalWorkLocations = locationsRes.totalCount || 0;
      }

      if (shiftsRes?.success) {
        this.stats.todayShifts = shiftsRes.totalCount || 0;
        this.stats.upcomingShifts = Math.floor(shiftsRes.totalCount * 0.3);
        this.stats.completedShifts = Math.floor(shiftsRes.totalCount * 0.7);
      }

      // Calculate derived stats
      this.stats.employeeGrowth = Math.floor(Math.random() * 15) + 5; // Mock data
      this.stats.shiftCompletion = Math.floor(Math.random() * 20) + 80; // Mock data
      this.stats.totalHours = this.stats.completedShifts * 8; // Estimate
      this.stats.avgShiftDuration = 8.5; // Standard shift

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
      const today = new Date().toISOString().split('T')[0];
      const response = await this.workShiftService.getWorkShifts(1, 10, undefined, today, today).toPromise();
      
      if (response?.success) {
        this.todayShifts = response.data.slice(0, 5);
      }
    } catch (error) {
      console.error('Error loading today shifts:', error);
    }
  }

  private loadRecentActivities(): void {
    // Mock activity data - in real app, this would come from an audit log service
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
      },
      {
        id: '3',
        type: 'system',
        title: 'Backup dữ liệu thành công',
        description: 'Hệ thống đã sao lưu dữ liệu lúc 2:00 AM',
        timestamp: new Date(Date.now() - 120 * 60000),
        user: 'System',
        icon: '💾',
        color: 'secondary'
      }
    ];
  }

  // === ANIMATIONS ===

  private animateNumbers(): void {
    const duration = 2000; // 2 seconds
    const steps = 60;
    const stepDuration = duration / steps;

    const stats = [
      { key: 'totalEmployees', target: this.stats.totalEmployees },
      { key: 'activeEmployees', target: this.stats.activeEmployees },
      { key: 'totalDepartments', target: this.stats.totalDepartments },
      { key: 'todayShifts', target: this.stats.todayShifts },
      { key: 'upcomingShifts', target: this.stats.upcomingShifts },
      { key: 'completedShifts', target: this.stats.completedShifts }
    ];

    stats.forEach(stat => {
      this.animatedNumbers[stat.key] = 0;
      const increment = stat.target / steps;

      interval(stepDuration)
        .pipe(
          takeUntil(this.destroy$),
          map(step => Math.min(Math.floor(increment * (step + 1)), stat.target))
        )
        .subscribe(value => {
          this.animatedNumbers[stat.key] = value;
          if (value >= stat.target) {
            this.animatedNumbers[stat.key] = stat.target;
          }
        });
    });
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

    // Add haptic feedback if available
    if ('vibrate' in navigator) {
      navigator.vibrate(50);
    }

    // Navigate to action route
    this.router.navigate([action.route]);
    
    // Show feedback
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
    // Future implementation - activity log page
    this.notificationService.showInfo(
      'Đang phát triển',
      'Trang nhật ký hoạt động sẽ sớm được cập nhật.'
    );
  }

  // === UTILITY METHODS ===

  getAnimatedNumber(key: string): number {
    return this.animatedNumbers[key] || 0;
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
}
