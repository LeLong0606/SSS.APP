import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> => {
  // Get stored token
  const authService = inject(AuthService);
  const token = authService.getStoredToken();

  // Set default headers
  let headers = req.headers;
  
  // Ensure Content-Type is set for POST/PUT requests
  if ((req.method === 'POST' || req.method === 'PUT') && !headers.has('Content-Type')) {
    headers = headers.set('Content-Type', 'application/json');
  }

  // Add authorization header if token exists and not in skip list
  if (token && !shouldSkipAuth(req.url)) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  // Clone request with updated headers
  const authReq = req.clone({ headers });
  return next(authReq);
};

function shouldSkipAuth(url: string): boolean {
  const skipUrls = [
    '/auth/login',
    '/auth/register',
    '/auth/refresh-token'
  ];

  return skipUrls.some(skipUrl => url.includes(skipUrl));
}
