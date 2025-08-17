import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  // Skip auth header for certain requests
  if (shouldSkipAuth(req.url)) {
    return next(req);
  }

  // Get stored token
  const authService = inject(AuthService);
  const token = authService.getStoredToken();

  // Clone request and add authorization header if token exists
  if (token) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(authReq);
  }

  return next(req);
};

function shouldSkipAuth(url: string): boolean {
  const skipUrls = [
    '/auth/login',
    '/auth/register',
    '/auth/refresh-token'
  ];

  return skipUrls.some(skipUrl => url.includes(skipUrl));
}
