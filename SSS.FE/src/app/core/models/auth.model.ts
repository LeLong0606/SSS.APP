import { BaseEntity } from './api-response.model';

// User roles enum
export enum UserRole {
  ADMINISTRATOR = 'Administrator',
  DIRECTOR = 'Director',
  TEAM_LEADER = 'TeamLeader',
  EMPLOYEE = 'Employee'
}

// Login request
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

// Register request  
export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  employeeCode?: string;
  role: UserRole;
}

// Change password request
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// Refresh token request
export interface RefreshTokenRequest {
  refreshToken: string;
}

// Authentication response
export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  refreshToken?: string;
  expiresAt?: Date;
  user?: UserInfo;
  errors?: string[];
}

// User information
export interface UserInfo extends BaseEntity {
  email: string;
  fullName: string;
  employeeCode?: string;
  roles: UserRole[];
  isActive: boolean;
  avatar?: string;
  department?: string;
  position?: string;
}

// Current user state
export interface AuthState {
  isAuthenticated: boolean;
  user: UserInfo | null;
  token: string | null;
  refreshToken: string | null;
  permissions: string[];
  loading: boolean;
  error: string | null;
}

// Login form data
export interface LoginFormData {
  email: string;
  password: string;
  rememberMe: boolean;
}

// Register form data
export interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  employeeCode: string;
  role: UserRole;
  acceptTerms: boolean;
}
