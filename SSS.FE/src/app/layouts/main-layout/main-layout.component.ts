import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserInfo, UserRole } from '../../core/models/auth.model';
import { environment } from '../../../environments/environment';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  requiredRoles?: UserRole[];
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
  isLoading = false;
  isSidebarExpanded = true;
  appName = environment.appName;
  appVersion = environment.version;

  menuItems: MenuItem[] = [
    {
      label: 'Trang chủ',
      icon: 'dashboard',
      route: '/dashboard',
      requiredRoles: [UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
    },
    {
      label: 'Quản lý nhân viên',
      icon: 'people',
      route: '/employees',
      requiredRoles: [UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
    },
    {
      label: 'Quản lý phòng ban',
      icon: 'business',
      route: '/departments',
      requiredRoles: [UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
    },
    {
      label: 'Quản lý ca làm việc',
      icon: 'schedule',
      route: '/work-shifts',
      requiredRoles: [UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
    },
    {
      label: 'Hồ sơ cá nhân',
      icon: 'account_circle',
      route: '/profile',
      requiredRoles: [UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
    },
    {
      label: 'Quản trị hệ thống',
      icon: 'settings',
      route: '/admin',
      requiredRoles: [UserRole.ADMINISTRATOR]
    }
  ];

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to auth state changes
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(state => {
        this.currentUser = state.user;
        this.isLoading = state.loading;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Check if user has access to menu item
  hasAccess(menuItem: MenuItem): boolean {
    if (!menuItem.requiredRoles || menuItem.requiredRoles.length === 0) {
      return true;
    }

    return this.authService.hasAnyRole(menuItem.requiredRoles);
  }

  // Get visible menu items based on user roles
  getVisibleMenuItems(): MenuItem[] {
    return this.menuItems.filter(item => this.hasAccess(item));
  }

  // Toggle sidebar
  toggleSidebar(): void {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  // Navigate to route
  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

  // Logout
  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.notificationService.showSuccess('Đăng xuất thành công');
      },
      error: () => {
        // Even if logout API fails, we still redirect to login
        this.notificationService.showInfo('Đã đăng xuất khỏi hệ thống');
      }
    });
  }

  // Get user display name
  getUserDisplayName(): string {
    return this.currentUser?.fullName || this.currentUser?.email || 'User';
  }

  // Get user role display
  getUserRoleDisplay(): string {
    if (!this.currentUser?.roles || this.currentUser.roles.length === 0) {
      return 'Người dùng';
    }

    const roleMap = {
      [UserRole.ADMINISTRATOR]: 'Quản trị viên',
      [UserRole.DIRECTOR]: 'Giám đốc',
      [UserRole.TEAM_LEADER]: 'Trưởng nhóm',
      [UserRole.EMPLOYEE]: 'Nhân viên'
    };

    // Get the highest role
    const highestRole = this.currentUser.roles[0];
    return roleMap[highestRole] || 'Người dùng';
  }

  // Get user avatar
  getUserAvatar(): string {
    return this.currentUser?.avatar || '/assets/images/avatar-default.png';
  }
}
