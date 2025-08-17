import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import {
  LoginRequest,
  RegisterRequest,
  ChangePasswordRequest,
  RefreshTokenRequest,
  AuthResponse,
  UserInfo,
  AuthState,
  UserRole
} from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = environment.apiUrl;
  private readonly TOKEN_KEY = environment.storage.tokenKey;
  private readonly REFRESH_TOKEN_KEY = environment.storage.refreshTokenKey;
  private readonly USER_KEY = environment.storage.userKey;

  // Auth state management
  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    user: null,
    token: null,
    refreshToken: null,
    permissions: [],
    loading: false,
    error: null
  });

  public authState$ = this.authStateSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.initializeAuthState();
  }

  // Initialize auth state from storage
  private initializeAuthState(): void {
    const token = this.getStoredToken();
    const refreshToken = this.getStoredRefreshToken();
    const user = this.getStoredUser();

    if (token && user) {
      this.updateAuthState({
        isAuthenticated: true,
        user,
        token,
        refreshToken,
        permissions: this.getUserPermissions(user),
        loading: false,
        error: null
      });
    }
  }

  // Login
  login(credentials: LoginRequest): Observable<AuthResponse> {
    this.setLoading(true);
    
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/login`, credentials).pipe(
      tap(response => {
        if (response.success && response.token && response.user) {
          this.handleSuccessfulAuth(response);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }

  // Register
  register(userData: RegisterRequest): Observable<AuthResponse> {
    this.setLoading(true);
    
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/register`, userData).pipe(
      tap(response => {
        if (response.success && response.token && response.user) {
          this.handleSuccessfulAuth(response);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }

  // Logout
  logout(): Observable<any> {
    return this.http.post(`${this.API_URL}/auth/logout`, {}).pipe(
      tap(() => this.handleLogout()),
      catchError(() => {
        // Even if server call fails, clear local auth state
        this.handleLogout();
        return of(null);
      })
    );
  }

  // Refresh token
  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getStoredRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const request: RefreshTokenRequest = { refreshToken };
    
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/refresh-token`, request).pipe(
      tap(response => {
        if (response.success && response.token) {
          this.updateTokens(response.token, response.refreshToken || null);
        }
      }),
      catchError(error => {
        this.handleLogout();
        return throwError(() => error);
      })
    );
  }

  // Change password
  changePassword(passwordData: ChangePasswordRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/auth/change-password`, passwordData).pipe(
      catchError(error => this.handleAuthError(error))
    );
  }

  // Get current user info
  getCurrentUser(): Observable<AuthResponse> {
    return this.http.get<AuthResponse>(`${this.API_URL}/auth/me`).pipe(
      tap(response => {
        if (response.success && response.user) {
          this.updateUser(response.user);
        }
      }),
      catchError(error => this.handleAuthError(error))
    );
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    const currentState = this.authStateSubject.value;
    return currentState.isAuthenticated && !!currentState.token && !!currentState.user;
  }

  // Check if user has specific role
  hasRole(role: UserRole): boolean {
    const user = this.getCurrentUserSync();
    return user?.roles?.includes(role) || false;
  }

  // Check if user has any of the specified roles
  hasAnyRole(roles: UserRole[]): boolean {
    const user = this.getCurrentUserSync();
    return user?.roles?.some(role => roles.includes(role)) || false;
  }

  // Check if user has permission
  hasPermission(permission: string): boolean {
    const currentState = this.authStateSubject.value;
    return currentState.permissions.includes(permission);
  }

  // Get current user synchronously
  getCurrentUserSync(): UserInfo | null {
    return this.authStateSubject.value.user;
  }

  // Get stored token
  getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  // Get stored refresh token
  getStoredRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  // Get stored user
  private getStoredUser(): UserInfo | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    return null;
  }

  // Handle successful authentication
  private handleSuccessfulAuth(response: AuthResponse): void {
    if (response.token && response.user) {
      this.storeTokens(response.token, response.refreshToken || null);
      this.storeUser(response.user);
      
      this.updateAuthState({
        isAuthenticated: true,
        user: response.user,
        token: response.token,
        refreshToken: response.refreshToken || null,
        permissions: this.getUserPermissions(response.user),
        loading: false,
        error: null
      });
    }
  }

  // Handle logout
  private handleLogout(): void {
    this.clearStorage();
    
    this.updateAuthState({
      isAuthenticated: false,
      user: null,
      token: null,
      refreshToken: null,
      permissions: [],
      loading: false,
      error: null
    });

    this.router.navigate(['/auth/login']);
  }

  // Handle auth errors
  private handleAuthError(error: HttpErrorResponse): Observable<never> {
    this.setLoading(false);
    
    let errorMessage = 'An authentication error occurred';
    
    if (error.error && error.error.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }

    this.setError(errorMessage);
    
    // If unauthorized, logout user
    if (error.status === 401) {
      this.handleLogout();
    }
    
    return throwError(() => error);
  }

  // Store tokens
  private storeTokens(token: string, refreshToken: string | null): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    if (refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
    }
  }

  // Store user info
  private storeUser(user: UserInfo): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  // Update tokens only
  private updateTokens(token: string, refreshToken: string | null): void {
    this.storeTokens(token, refreshToken);
    
    const currentState = this.authStateSubject.value;
    this.updateAuthState({
      ...currentState,
      token,
      refreshToken: refreshToken || currentState.refreshToken
    });
  }

  // Update user info
  private updateUser(user: UserInfo): void {
    this.storeUser(user);
    
    const currentState = this.authStateSubject.value;
    this.updateAuthState({
      ...currentState,
      user,
      permissions: this.getUserPermissions(user)
    });
  }

  // Clear storage
  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  // Update auth state
  private updateAuthState(newState: AuthState): void {
    this.authStateSubject.next(newState);
  }

  // Set loading state
  private setLoading(loading: boolean): void {
    const currentState = this.authStateSubject.value;
    this.updateAuthState({
      ...currentState,
      loading,
      error: loading ? null : currentState.error
    });
  }

  // Set error state
  private setError(error: string): void {
    const currentState = this.authStateSubject.value;
    this.updateAuthState({
      ...currentState,
      error,
      loading: false
    });
  }

  // Get user permissions based on roles
  private getUserPermissions(user: UserInfo): string[] {
    const permissions: string[] = [];
    
    user.roles.forEach(role => {
      switch (role) {
        case UserRole.ADMINISTRATOR:
          permissions.push(
            'users.create', 'users.read', 'users.update', 'users.delete',
            'employees.create', 'employees.read', 'employees.update', 'employees.delete',
            'departments.create', 'departments.read', 'departments.update', 'departments.delete',
            'workShifts.create', 'workShifts.read', 'workShifts.update', 'workShifts.delete',
            'system.admin'
          );
          break;
          
        case UserRole.DIRECTOR:
          permissions.push(
            'employees.create', 'employees.read', 'employees.update', 'employees.delete',
            'departments.create', 'departments.read', 'departments.update',
            'workShifts.create', 'workShifts.read', 'workShifts.update', 'workShifts.delete'
          );
          break;
          
        case UserRole.TEAM_LEADER:
          permissions.push(
            'employees.create', 'employees.read', 'employees.update',
            'departments.read',
            'workShifts.create', 'workShifts.read', 'workShifts.update'
          );
          break;
          
        case UserRole.EMPLOYEE:
          permissions.push(
            'employees.read',
            'departments.read',
            'workShifts.read',
            'profile.update'
          );
          break;
      }
    });
    
    return [...new Set(permissions)]; // Remove duplicates
  }
}
