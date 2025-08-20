// Core Services Export
export * from './auth.service';
export * from './employee.service';
export * from './department.service';
export * from './work-shift.service';
export * from './work-location.service';
export * from './attendance.service';
export * from './payroll.service';
export * from './shift-management.service';
export * from './image.service';
export * from './loading.service';
export * from './notification.service';

// Export DashboardService with specific interfaces to avoid conflicts
export { DashboardService } from './dashboard.service';
export type { DashboardStats, DashboardAttendanceStatus, RecentActivity } from './dashboard.service';

// Service Interfaces and Models
export * from '../models/api-response.model';
export * from '../models/auth.model';
export * from '../models/employee.model';
export * from '../models/department.model';
export * from '../models/work-location.model';
export * from '../models/work-shift.model';

// Interceptors
export * from '../interceptors/auth.interceptor';
export * from '../interceptors/loading.interceptor';
