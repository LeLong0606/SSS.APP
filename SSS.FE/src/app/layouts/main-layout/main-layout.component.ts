import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { filter } from 'rxjs/operators'; // ✅ FIX: Import filter từ operators

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

  // ✅ FIX: Thêm tất cả thuộc tính bị thiếu
  currentUser: UserInfo | null = null;
  isSidebarCollapsed = false;
  isMobileView = false; // ✅ FIX: Thêm thuộc tính thiếu
  isSidebarExpanded = true; // ✅ FIX: Thêm thuộc tính thiếu
  unreadNotifications = 0;
  currentRoute = '';
  isLoading = false;
  appName = 'SSS Employee Management';

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
    }
  ];

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
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
        this.updateMenuItems();
      });

    this.notificationService.unreadCount$
      .pipe(takeUntil(this.destroy$))
      .subscribe((count: number) => { // ✅ FIX: Explicit type
        this.unreadNotifications = count;
      });
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
    // ✅ FIX: Đúng cách sử dụng filter với type guard
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd), // ✅ FIX: Type guard
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => { // ✅ FIX: Now safe to type as NavigationEnd
        this.currentRoute = event.urlAfterRedirects;
      });
  }

  // Sidebar methods
  toggleSidebar(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  closeSidebar(): void {
    if (this.isMobileView) {
      this.isSidebarExpanded = false;
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
    return this.getUserDisplayName().charAt(0).toUpperCase();
  }

  // Action methods
  goToProfile(): void {
    this.router.navigate(['/profile']);
    this.closeSidebar();
  }

  logout(): void {
    if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
      this.authService.logout()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (): void => { // ✅ FIX: Explicit return type
            this.notificationService.showSuccess('Đăng xuất thành công');
            this.router.navigate(['/auth/login']);
          },
          error: (error: any): void => { // ✅ FIX: Explicit parameter type
            console.error('Logout error:', error);
            this.notificationService.showError('Có lỗi khi đăng xuất');
            this.router.navigate(['/auth/login']);
          }
        });
    }
  }

  // Notification methods
  openNotifications(): void {
    this.notificationService.showInfo('Tính năng thông báo sẽ được cập nhật');
  }

  // Template helper methods
  trackByMenuItem(index: number, item: MenuItem): string {
    return item.route;
  }
}
