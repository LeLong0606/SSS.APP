// User roles enum - EXACT match with backend
export enum UserRole {
  ADMINISTRATOR = 'Administrator',
  DIRECTOR = 'Director',
  TEAM_LEADER = 'TeamLeader',
  EMPLOYEE = 'Employee'
}

// ✅ FIXED: Login request - EXACT match with backend LoginRequest (NO rememberMe!)
export interface LoginRequest {
  email: string;
  password: string;
  // rememberMe removed - backend doesn't support it
}

// ✅ FIXED: Register request - EXACT match with backend RegisterRequest  
export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  employeeCode?: string;
  role: string; // ✅ FIX: STRING, not UserRole enum to match backend!
}

// Change password request - EXACT match with backend ChangePasswordRequest
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// Refresh token request - EXACT match with backend RefreshTokenRequest
export interface RefreshTokenRequest {
  refreshToken: string;
}

// Authentication response - EXACT match with backend AuthResponse
export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  refreshToken?: string;
  expiresAt?: Date;
  user?: UserInfo;
  errors?: string[];
}

// ✅ FIXED: User information - EXACT match with backend UserInfo
export interface UserInfo {
  id: string; // ✅ FIX: STRING, not number! Backend uses ApplicationUser.Id (GUID)
  email: string;
  fullName: string;
  employeeCode?: string;
  roles: string[]; // ✅ FIX: STRING[], not UserRole[] to match backend List<string>
  isActive: boolean;
  createdAt: Date;
  // ✅ FIX: Removed extra fields not in backend (avatar, department, position)
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

// ✅ FIXED: Login form data - REMOVED rememberMe
export interface LoginFormData {
  email: string;
  password: string;
  // ✅ FIX: rememberMe removed
}

// ✅ FIXED: Register form data - role as string
export interface RegisterFormData {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  employeeCode: string;
  role: string; // ✅ FIX: STRING to match backend
  acceptTerms: boolean;
}

// NEW: Revoke token request - EXACT match with backend RevokeTokenRequest
export interface RevokeTokenRequest {
  refreshToken?: string;
}
