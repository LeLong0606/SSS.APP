// API Response wrapper
export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

// Paginated response
export interface PagedResponse<T = any> extends ApiResponse<T[]> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Base entity interface
export interface BaseEntity {
  id: string;
  createdAt: Date;
  updatedAt?: Date;
}

// Error response
export interface ErrorResponse {
  message: string;
  statusCode: number;
  timestamp: string;
  path: string;
  errors?: { [key: string]: string[] };
}

// Pagination parameters
export interface PaginationParams {
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

// Search parameters
export interface SearchParams extends PaginationParams {
  searchTerm?: string;
  filters?: { [key: string]: any };
}
