// API Response interfaces that match backend exactly
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors: string[];
}

export interface PagedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  errors: string[];
}

// Base entity interface for all entities
export interface BaseEntity {
  id: number;
  createdAt: Date;
  updatedAt?: Date;
}

// Filter interface for API requests
export interface ApiFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}

// Common API request options
export interface ApiRequestOptions {
  includeInactive?: boolean;
  includeEmployees?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

// Error response structure
export interface ApiError {
  message: string;
  errors: string[];
  status: number;
  timestamp: string;
}
