// Work Shift related interfaces - EXACT match with backend DTOs
export interface WorkShift {
  id: number; // ✅ FIX: NUMBER, not string! Backend uses int
  employeeCode: string;
  employeeName: string;
  employeeDepartment?: string;
  workLocationId: number; // ✅ FIX: NUMBER, not string! Backend uses int
  workLocationName: string;
  workLocationCode?: string;
  workLocationAddress?: string;
  shiftDate: Date;
  startTime: string; // HH:mm format
  endTime: string; // HH:mm format
  totalHours: number;
  assignedByEmployeeCode: string;
  assignedByEmployeeName: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
  // Modification tracking
  isModified: boolean;
  modifiedByEmployeeCode?: string;
  modifiedByEmployeeName?: string;
  modifiedAt?: Date;
  modificationReason?: string;
}

// ✅ FIXED: Create work shift request - EXACT match with backend CreateWorkShiftRequest
export interface CreateWorkShiftRequest {
  employeeCode: string;
  workLocationId: number; // ✅ FIX: NUMBER, not string! Backend expects int
  shiftDate: Date; // ✅ FIX: DATE object, not string! Backend expects DateTime
  startTime: string; // HH:mm format (converted to TimeOnly in backend)
  endTime: string; // HH:mm format (converted to TimeOnly in backend)
}

// ✅ FIXED: Update work shift request - EXACT match with backend UpdateWorkShiftRequest
export interface UpdateWorkShiftRequest {
  workLocationId: number; // ✅ FIX: NUMBER, not string!
  startTime: string; // TimeOnly format HH:mm
  endTime: string; // TimeOnly format HH:mm
  modificationReason?: string;
}

// ✅ FIXED: Weekly shift request - EXACT match with backend CreateWeeklyShiftsRequest
export interface WeeklyShiftRequest {
  employeeCode: string;
  weekStartDate: Date; // ✅ FIX: Must be Monday, DATE object not string
  dailyShifts: DailyShiftRequest[];
}

// ✅ FIXED: Daily shift request - EXACT match with backend DailyShiftRequest
export interface DailyShiftRequest {
  dayOfWeek: number; // 1=Monday, 7=Sunday (backend expects this format)
  workLocationId: number; // ✅ FIX: NUMBER, not string!
  startTime: string; // TimeOnly format HH:mm
  endTime: string; // TimeOnly format HH:mm
}

// Weekly shifts DTO - EXACT match with backend WeeklyShiftsDto
export interface WeeklyShiftsDto {
  employeeCode: string;
  employeeName: string;
  weekStartDate: Date;
  weekEndDate: Date;
  totalWeeklyHours: number;
  dailyShifts: WorkShift[];
}

// Work shift log DTO - EXACT match with backend WorkShiftLogDto
export interface WorkShiftLogDto {
  id: number;
  workShiftId: number;
  action: string;
  performedByEmployeeCode: string;
  performedByEmployeeName: string;
  performedAt: Date;
  originalValues?: string;
  newValues?: string;
  reason?: string;
  comments?: string;
}

// Shift validation request - EXACT match with backend ShiftValidationRequest
export interface ShiftValidationRequest {
  employeeCode: string;
  shiftDate: Date;
  startTime: string;
  endTime: string;
  excludeShiftId?: number; // ✅ FIX: NUMBER, not string!
}

// Shift validation response - EXACT match with backend ShiftValidationResponse
export interface ShiftValidationResponse {
  isValid: boolean;
  validationErrors: string[];
  totalDailyHours: number;
  conflictingShifts: WorkShift[];
}

// Filter interfaces for API calls
export interface WorkShiftFilter {
  employeeCode?: string;
  startDate?: string; // ISO string format for API
  endDate?: string; // ISO string format for API
  locationId?: number; // ✅ FIX: NUMBER, not string!
  isActive?: boolean;
}

// Utility interfaces for frontend display
export interface WorkShiftListItem {
  id: number; // ✅ FIX: NUMBER, not string!
  employeeCode: string;
  employeeName: string;
  shiftDate: Date;
  startTime: string;
  endTime: string;
  workLocationName: string;
  duration: string; // Calculated duration in hours
  isActive: boolean;
}

export interface WorkShiftStats {
  totalShifts: number;
  activeShifts: number;
  upcomingShifts: number;
  completedShifts: number;
  totalHours: number;
  averageShiftDuration: number;
  employeesWithShifts: number;
  locationsInUse: number;
}

export interface ShiftCalendarEvent {
  id: number; // ✅ FIX: NUMBER, not string!
  title: string;
  start: Date;
  end: Date;
  employeeCode: string;
  employeeName: string;
  locationName: string;
  backgroundColor?: string;
  textColor?: string;
}

export interface ShiftConflict {
  conflictType: 'OVERLAP' | 'DOUBLE_BOOKING' | 'LOCATION_CAPACITY';
  message: string;
  conflictingShift: WorkShift;
  severity: 'HIGH' | 'MEDIUM' | 'LOW';
}

// Enums for better type safety
export enum ShiftType {
  MORNING = 'MORNING',
  AFTERNOON = 'AFTERNOON',
  EVENING = 'EVENING',
  NIGHT = 'NIGHT',
  FULL_DAY = 'FULL_DAY',
  OVERTIME = 'OVERTIME'
}

// ✅ FIXED: Day of week to match backend (1=Monday, not 0=Sunday)
export enum DayOfWeek {
  MONDAY = 1,
  TUESDAY = 2,
  WEDNESDAY = 3,
  THURSDAY = 4,
  FRIDAY = 5,
  SATURDAY = 6,
  SUNDAY = 7
}
