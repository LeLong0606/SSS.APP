// Work Location interfaces - EXACT match with backend DTOs
export interface WorkLocation {
  id: number; // ✅ FIX: NUMBER to match backend int
  name: string;
  locationCode?: string;
  address?: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

// ✅ FIXED: Create work location request - EXACT match with backend CreateWorkLocationRequest
export interface CreateWorkLocationRequest {
  name: string;
  locationCode?: string;
  address?: string;
  description?: string;
}

// ✅ FIXED: Update work location request - EXACT match with backend UpdateWorkLocationRequest
export interface UpdateWorkLocationRequest {
  name: string;
  locationCode?: string;
  address?: string;
  description?: string;
  isActive?: boolean;
}

// Work location list item for tables
export interface WorkLocationListItem {
  id: number; // ✅ FIX: NUMBER to match backend
  name: string;
  locationCode?: string;
  address?: string;
  isActive: boolean;
  createdAt: Date;
}

// Work location filter options
export interface WorkLocationFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

// Work location statistics
export interface WorkLocationStats {
  totalLocations: number;
  activeLocations: number;
  inactiveLocations: number;
  locationsWithShifts: number;
  averageShiftsPerLocation: number;
}

// Work location form validation
export interface WorkLocationFormErrors {
  name?: string;
  locationCode?: string;
  address?: string;
  description?: string;
}

// Enums for location types (if needed in future)
export enum LocationType {
  OFFICE = 'OFFICE',
  WAREHOUSE = 'WAREHOUSE',
  STORE = 'STORE',
  FACTORY = 'FACTORY',
  REMOTE = 'REMOTE'
}

export enum LocationFacility {
  PARKING = 'PARKING',
  CAFETERIA = 'CAFETERIA',
  GYM = 'GYM',
  MEDICAL = 'MEDICAL',
  SECURITY = 'SECURITY'
}
