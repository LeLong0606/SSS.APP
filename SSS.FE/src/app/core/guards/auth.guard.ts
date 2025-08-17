import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | boolean {
    
    // Check if user is authenticated
    if (this.authService.isAuthenticated()) {
      return true;
    }

    // Try to get current user info (in case token exists but user state is not loaded)
    return this.authService.getCurrentUser().pipe(
      map(response => {
        if (response.success && response.user) {
          return true;
        } else {
          this.redirectToLogin(state.url);
          return false;
        }
      }),
      catchError(() => {
        this.redirectToLogin(state.url);
        return of(false);
      })
    );
  }

  private redirectToLogin(returnUrl: string): void {
    this.notificationService.showWarning('Vui lòng đăng nhập để tiếp tục');
    this.router.navigate(['/auth/login'], { 
      queryParams: { returnUrl } 
    });
  }
}
