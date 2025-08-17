import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, Observable } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

let isRefreshing = false;

export const errorInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse): Observable<HttpEvent<unknown>> => {
      // Handle different error status codes
      switch (error.status) {
        case 401:
          return handle401Error(req, next, error, authService, notificationService);
        case 403:
          return handle403Error(error, notificationService);
        case 404:
          return handle404Error(error, notificationService);
        case 500:
          return handle500Error(error, notificationService);
        default:
          return handleGeneralError(error, notificationService);
      }
    })
  );
};

function handle401Error(
  req: HttpRequest<unknown>, 
  next: HttpHandlerFn, 
  error: HttpErrorResponse,
  authService: AuthService, 
  notificationService: NotificationService
): Observable<HttpEvent<unknown>> {
  // If this is a refresh token request or login request, don't retry
  if (req.url.includes('/auth/refresh-token') || req.url.includes('/auth/login')) {
    notificationService.showError('Thông tin đăng nhập không hợp lệ');
    return throwError(() => error);
  }

  // Try to refresh token if not already refreshing
  if (!isRefreshing) {
    isRefreshing = true;
    
    return authService.refreshToken().pipe(
      switchMap((response) => {
        isRefreshing = false;
        
        if (response.success && response.token) {
          // Retry original request with new token
          const authReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${response.token}`)
          });
          return next(authReq);
        }
        
        return throwError(() => error);
      }),
      catchError((refreshError) => {
        isRefreshing = false;
        notificationService.showError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        return throwError(() => refreshError);
      })
    );
  }

  return throwError(() => error);
}

function handle403Error(error: HttpErrorResponse, notificationService: NotificationService): Observable<HttpEvent<unknown>> {
  notificationService.showError('Bạn không có quyền truy cập chức năng này');
  return throwError(() => error);
}

function handle404Error(error: HttpErrorResponse, notificationService: NotificationService): Observable<HttpEvent<unknown>> {
  notificationService.showError('Không tìm thấy tài nguyên yêu cầu');
  return throwError(() => error);
}

function handle500Error(error: HttpErrorResponse, notificationService: NotificationService): Observable<HttpEvent<unknown>> {
  notificationService.showError('Lỗi server. Vui lòng thử lại sau.');
  return throwError(() => error);
}

function handleGeneralError(error: HttpErrorResponse, notificationService: NotificationService): Observable<HttpEvent<unknown>> {
  let errorMessage = 'Đã xảy ra lỗi không xác định';
  
  if (error.error && error.error.message) {
    errorMessage = error.error.message;
  } else if (error.message) {
    errorMessage = error.message;
  }
  
  notificationService.showError(errorMessage);
  return throwError(() => error);
}
