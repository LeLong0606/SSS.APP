import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { UserRole } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean {
    
    // Check if user is authenticated first
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    // Get required roles from route data
    const requiredRoles = route.data?.['roles'] as UserRole[] || [];
    const requiredPermissions = route.data?.['permissions'] as string[] || [];

    // If no specific roles/permissions required, allow access
    if (requiredRoles.length === 0 && requiredPermissions.length === 0) {
      return true;
    }

    // Check role-based access
    if (requiredRoles.length > 0 && this.authService.hasAnyRole(requiredRoles)) {
      return true;
    }

    // Check permission-based access
    if (requiredPermissions.length > 0) {
      const hasPermission = requiredPermissions.every(permission =>
        this.authService.hasPermission(permission)
      );
      
      if (hasPermission) {
        return true;
      }
    }

    // Access denied
    this.notificationService.showError(
      'Bạn không có quyền truy cập vào trang này',
      'Quyền truy cập bị từ chối'
    );
    
    this.router.navigate(['/dashboard']);
    return false;
  }
}
