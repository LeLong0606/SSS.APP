import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, takeUntil, filter } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserInfo, UserRole } from '../../core/models/auth.model';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  requiredRoles?: UserRole[];
  badge?: string;
  children?: MenuItem[];
}

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  styleUrls: ['./main-layout.component.scss'],
  standalone: false
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  currentUser: UserInfo | null = null;
  isSidebarExpanded = true;
  isSidebarCollapsed = false; // Add this property
  isMobileView = false;
  currentRoute = '';
  isLoading = false;
  appName = 'SSS Employee Management';
  unreadNotifications = 0; // Add this property

  // Menu items based on user roles
  menuItems: MenuItem[] = [
    {
      label: 'Trang chủ',
      icon: '🏠',
      route: '/dashboard'
    },
    {
      label: 'Quản lý nhân viên',
      icon: '👥',
      route: '/employees',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      label: 'Phòng ban',
      icon: '🏢',
      route: '/departments',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      label: 'Ca làm việc',
      icon: '📅',
      route: '/work-shifts',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]
    },
    {
      label: 'Địa điểm làm việc',
      icon: '📍',
      route: '/work-locations',
      requiredRoles: [UserRole.ADMINISTRATOR, UserRole.DIRECTOR]
    },
    {
      label: 'Hồ sơ cá nhân',
      icon: '👤',
      route: '/profile'
    },
    {
      label: 'Quản trị hệ thống',
      icon: '⚙️',
      route: '/admin',
      requiredRoles: [UserRole.ADMINISTRATOR]
    }
  ];

  // Filtered menu based on user permissions
  filteredMenuItems: MenuItem[] = [];

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeUser();
    this.initializeResponsive();
    this.trackRouteChanges();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeUser(): void {
    // Subscribe to auth state changes
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
        this.updateMenuItems();
      });

    // Get current user if not already loaded
    if (!this.currentUser) {
      this.authService.getCurrentUser()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success && response.user) {
              this.currentUser = response.user;
              this.updateMenuItems();
            }
          },
          error: (error) => {
            console.error('Error loading current user:', error);
          }
        });
    }
  }

  private updateMenuItems(): void {
    if (!this.currentUser) {
      this.filteredMenuItems = [];
      return;
    }

    this.filteredMenuItems = this.menuItems.filter(item => {
      if (!item.requiredRoles || item.requiredRoles.length === 0) {
        return true;
      }

      return this.authService.hasAnyRole(item.requiredRoles);
    });
  }

  private initializeResponsive(): void {
    this.checkScreenSize();
    window.addEventListener('resize', () => this.checkScreenSize());
  }

  private checkScreenSize(): void {
    this.isMobileView = window.innerWidth < 768;
    if (this.isMobileView) {
      this.isSidebarExpanded = false;
    }
  }

  private trackRouteChanges(): void {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.currentRoute = event.urlAfterRedirects;
      });
  }

  // Sidebar methods
  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
    this.isSidebarCollapsed = !this.isSidebarExpanded; // Keep in sync
  }

  closeSidebar(): void {
    if (this.isMobileView) {
      this.isSidebarExpanded = false;
      this.isSidebarCollapsed = true;
    }
  }

  // Navigation methods
  navigate(route: string): void {
    this.router.navigate([route]);
    this.closeSidebar();
  }

  isActiveRoute(route: string): boolean {
    if (route === '/dashboard') {
      return this.currentRoute === '/' || this.currentRoute === '/dashboard';
    }
    return this.currentRoute.startsWith(route);
  }

  // User methods
  getUserDisplayName(): string {
    if (!this.currentUser) return 'Người dùng';
    return this.currentUser.fullName || this.currentUser.email || 'Người dùng';
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

  getUserAvatar(): string {
    // Return default avatar or user's profile picture
    return this.currentUser?.avatar || 'assets/images/default-avatar.svg';
  }

  onAvatarError(event: Event): void {
    const target = event.target as HTMLImageElement;
    if (target) {
      target.style.display = 'none';
    }
  }

  shouldShowFallbackIcon(route: string): boolean {
    const iconRoutes = ['dashboard', 'employees', 'departments', 'work-shifts', 'work-locations', 'profile', 'admin'];
    return !iconRoutes.some(iconRoute => route.includes(iconRoute));
  }

  getRoleBadgeClass(): string {
    if (!this.currentUser || !this.currentUser.roles || this.currentUser.roles.length === 0) {
      return 'role-employee';
    }

    const role = this.currentUser.roles[0];
    switch (role) {
      case UserRole.ADMINISTRATOR:
        return 'role-admin';
      case UserRole.DIRECTOR:
        return 'role-director';
      case UserRole.TEAM_LEADER:
        return 'role-team-leader';
      case UserRole.EMPLOYEE:
      default:
        return 'role-employee';
    }
  }

  // Action methods
  goToProfile(): void {
    this.router.navigate(['/profile']);
    this.closeSidebar();
  }

  goToSettings(): void {
    if (this.authService.hasRole(UserRole.ADMINISTRATOR)) {
      this.router.navigate(['/admin/settings']);
    } else {
      this.router.navigate(['/profile/settings']);
    }
    this.closeSidebar();
  }

  logout(): void {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
      this.authService.logout()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.notificationService.showSuccess('Đăng xuất thành công');
            this.router.navigate(['/auth/login']);
          },
          error: (error) => {
            console.error('Logout error:', error);
            this.notificationService.showError('Có lỗi khi đăng xuất');
            // Still redirect to login even if logout API fails
            this.router.navigate(['/auth/login']);
          }
        });
    }
  }

  // Theme methods (for future implementation)
  toggleTheme(): void {
    // TODO: Implement theme switching
    this.notificationService.showInfo('Tính năng chuyển đổi giao diện sẽ được cập nhật');
  }

  // Notification methods
  hasNotifications(): boolean {
    // TODO: Implement notification system
    return false;
  }

  getNotificationCount(): number {
    // TODO: Implement notification counting
    return 0;
  }

  openNotifications(): void {
    // TODO: Implement notifications panel
    this.notificationService.showInfo('Tính năng thông báo sẽ được cập nhật');
  }

  // Help methods
  openHelp(): void {
    // TODO: Implement help system
    this.notificationService.showInfo('Tính năng trợ giúp sẽ được cập nhật');
  }

  // Quick actions
  quickCreateEmployee(): void {
    if (this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER])) {
      this.router.navigate(['/employees/create']);
    } else {
      this.notificationService.showError('Bạn không có quyền tạo nhân viên mới');
    }
  }

  quickCreateShift(): void {
    if (this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER])) {
      this.router.navigate(['/work-shifts/create']);
    } else {
      this.notificationService.showError('Bạn không có quyền tạo ca làm việc');
    }
  }

  // Statistics methods (for dashboard widgets in sidebar)
  getQuickStats(): any {
    // TODO: Implement quick statistics
    return {
      employees: 0,
      departments: 0,
      todayShifts: 0,
      locations: 0
    };
  }

  // Template helper methods
  getUserRoleDisplay(): string {
    if (!this.currentUser?.roles?.length) {
      return 'Chưa xác định';
    }
    
    const roleNames: Record<string, string> = {
      [UserRole.ADMINISTRATOR]: 'Quản trị viên',
      [UserRole.DIRECTOR]: 'Giám đốc',
      [UserRole.TEAM_LEADER]: 'Trưởng nhóm',
      [UserRole.EMPLOYEE]: 'Nhân viên'
    };
    
    return this.currentUser.roles
      .map(role => roleNames[role] || role)
      .join(', ');
  }

  getVisibleMenuItems(): MenuItem[] {
    return this.menuItems.filter(item => {
      if (!item.requiredRoles) return true;
      if (!this.currentUser) return false;
      return this.authService.hasAnyRole(item.requiredRoles);
    });
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }
}
