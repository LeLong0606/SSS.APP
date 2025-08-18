import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil, finalize } from 'rxjs';

import { WorkShiftService } from '../../core/services/work-shift.service';
import { WorkLocationService } from '../../core/services/work-location.service';
import { EmployeeService } from '../../core/services/employee.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { WorkShift, WorkShiftFilter, ShiftType } from '../../core/models/work-shift.model';
import { WorkLocation } from '../../core/models/work-location.model';
import { Employee } from '../../core/models/employee.model';
import { PagedResponse } from '../../core/models/api-response.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-work-shift-list',
  templateUrl: './work-shift-list.component.html',
  styleUrls: ['./work-shift-list.component.scss'],
  standalone: false
})
export class WorkShiftListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  // Data properties
  workShifts: WorkShift[] = [];
  workLocations: WorkLocation[] = [];
  employees: Employee[] = [];
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalRecords = 0;
  totalPages = 0;

  // Search and filters
  searchTerm = '';
  selectedEmployee = '';
  selectedLocation = '';
  selectedShiftType = '';
  startDate = '';
  endDate = '';
  
  // UI state
  isLoading = false;
  isSearching = false;
  showCreateForm = false;
  showWeeklyView = false;
  selectedShift: WorkShift | null = null;

  // Permissions
  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManageAll = false;

  // Filter options
  shiftTypeOptions = [
    { value: '', label: 'Tất cả ca làm' },
    { value: ShiftType.MORNING, label: 'Ca sáng' },
    { value: ShiftType.AFTERNOON, label: 'Ca chiều' },
    { value: ShiftType.EVENING, label: 'Ca tối' },
    { value: ShiftType.NIGHT, label: 'Ca đêm' },
    { value: ShiftType.FULL_DAY, label: 'Ca cả ngày' },
    { value: ShiftType.OVERTIME, label: 'Ca tăng ca' }
  ];

  constructor(
    private workShiftService: WorkShiftService,
    private workLocationService: WorkLocationService,
    private employeeService: EmployeeService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.initializeSearch();
    this.initializeDateFilters();
    this.loadWorkLocations();
    this.loadEmployees();
    this.loadWorkShifts();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canManageAll = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private initializeSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.currentPage = 1;
      this.loadWorkShifts();
    });
  }

  private initializeDateFilters(): void {
    // Default to current week
    const today = new Date();
    const monday = new Date(today);
    monday.setDate(today.getDate() - today.getDay() + 1);
    
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);

    this.startDate = monday.toISOString().split('T')[0];
    this.endDate = sunday.toISOString().split('T')[0];
  }

  private loadWorkLocations(): void {
    this.workLocationService.getAllWorkLocations()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.workLocations = response.data.filter(loc => loc.isActive);
          }
        },
        error: (error) => {
          console.error('Error loading work locations:', error);
        }
      });
  }

  private loadEmployees(): void {
    this.employeeService.getEmployees({ pageNumber: 1, pageSize: 100 }) // Get more employees for selection
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employees = response.data.filter((emp: Employee) => emp.isActive);
          }
        },
        error: (error) => {
          console.error('Error loading employees:', error);
        }
      });
  }

  loadWorkShifts(): void {
    this.isLoading = true;

    const employeeCode = this.selectedEmployee || undefined;
    const startDate = this.startDate || undefined;
    const endDate = this.endDate || undefined;
    // ✅ FIX: Convert selectedLocation to number
    const locationId = this.selectedLocation ? Number(this.selectedLocation) : undefined;

    this.workShiftService.getWorkShifts(
      this.currentPage, 
      this.pageSize, 
      employeeCode, 
      startDate, 
      endDate, 
      locationId
    )
      .pipe(
        finalize(() => this.isLoading = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.workShifts = response.data;
            this.totalRecords = response.totalCount || 0;
            this.totalPages = response.totalPages || 0;
          } else {
            this.workShifts = [];
            this.totalRecords = 0;
            this.totalPages = 0;
          }
        },
        error: (error) => {
          console.error('Error loading work shifts:', error);
          this.notificationService.showError('Lỗi khi tải danh sách ca làm việc');
          this.workShifts = [];
          this.totalRecords = 0;
          this.totalPages = 0;
        }
      });
  }

  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadWorkShifts();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadWorkShifts();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadWorkShifts();
  }

  // Work shift actions
  createWorkShift(): void {
    if (!this.canCreate) {
      this.notificationService.showError('Bạn không có quyền tạo ca làm việc mới');
      return;
    }
    
    this.router.navigate(['/work-shifts/create']);
  }

  createWeeklyShifts(): void {
    if (!this.canCreate) {
      this.notificationService.showError('Bạn không có quyền tạo ca làm việc theo tuần');
      return;
    }
    
    this.router.navigate(['/work-shifts/weekly']);
  }

  viewWorkShift(shift: WorkShift): void {
    this.router.navigate(['/work-shifts', shift.id]);
  }

  editWorkShift(shift: WorkShift): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa ca làm việc');
      return;
    }

    // Check if user can edit this specific shift
    if (!this.canManageAll && !this.canUserEditShift(shift)) {
      this.notificationService.showError('Bạn chỉ có thể chỉnh sửa ca làm việc của mình');
      return;
    }

    this.router.navigate(['/work-shifts', shift.id, 'edit']);
  }

  deleteWorkShift(shift: WorkShift): void {
    if (!this.canDelete) {
      this.notificationService.showError('Bạn không có quyền xóa ca làm việc');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa ca làm việc của ${shift.employeeName} vào ngày ${this.formatDate(shift.shiftDate)}?`)) {
      this.isLoading = true;
      
      this.workShiftService.deleteWorkShift(shift.id)
        .pipe(
          finalize(() => this.isLoading = false),
          takeUntil(this.destroy$)
        )
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa ca làm việc thành công');
              this.loadWorkShifts();
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa ca làm việc');
            }
          },
          error: (error) => {
            console.error('Error deleting work shift:', error);
            this.notificationService.showError('Lỗi khi xóa ca làm việc');
          }
        });
    }
  }

  // View toggles
  toggleWeeklyView(): void {
    this.showWeeklyView = !this.showWeeklyView;
    if (this.showWeeklyView) {
      this.router.navigate(['/work-shifts/weekly-view']);
    }
  }

  // Utility methods
  private canUserEditShift(shift: WorkShift): boolean {
    const currentUser = this.authService.getCurrentUserSync();
    if (!currentUser) return false;
    
    // Users can edit their own shifts or shifts they created
    return shift.employeeCode === currentUser.employeeCode || 
           shift.createdByEmployeeCode === currentUser.employeeCode;
  }

  // ✅ FIX: Change parameter type to number and handle comparison correctly
  getLocationName(locationId: number): string {
    const location = this.workLocations.find(loc => loc.id === locationId);
    return location ? location.name : 'Không xác định';
  }

  getEmployeeName(employeeCode: string): string {
    const employee = this.employees.find(emp => emp.employeeCode === employeeCode);
    return employee ? employee.fullName : employeeCode;
  }

  getShiftTypeText(shiftType?: string): string {
    const option = this.shiftTypeOptions.find(opt => opt.value === shiftType);
    return option ? option.label : (shiftType || 'Không xác định');
  }

  formatDate(date: Date | string): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN');
  }

  formatTime(time: string): string {
    if (!time) return '';
    return time.substring(0, 5); // HH:mm format
  }

  calculateDuration(startTime: string, endTime: string): string {
    if (!startTime || !endTime) return '';
    
    const start = new Date(`2000-01-01T${startTime}`);
    const end = new Date(`2000-01-01T${endTime}`);
    
    let diff = end.getTime() - start.getTime();
    if (diff < 0) diff += 24 * 60 * 60 * 1000; // Handle overnight shifts
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    
    return `${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`;
  }

  isUpcomingShift(shift: WorkShift): boolean {
    const shiftDate = new Date(shift.shiftDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    return shiftDate >= today;
  }

  isPastShift(shift: WorkShift): boolean {
    const shiftDate = new Date(shift.shiftDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    return shiftDate < today;
  }

  getShiftStatusClass(shift: WorkShift): string {
    if (this.isUpcomingShift(shift)) return 'shift-upcoming';
    if (this.isPastShift(shift)) return 'shift-past';
    return 'shift-today';
  }

  // Export functionality
  exportShifts(): void {
    if (!this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR])) {
      this.notificationService.showError('Bạn không có quyền xuất dữ liệu');
      return;
    }

    const startDate = this.startDate || undefined;
    const endDate = this.endDate || undefined;
    const employeeCode = this.selectedEmployee || undefined;

    this.workShiftService.exportShifts(startDate, endDate, employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `ca-lam-viec-${startDate || 'all'}-${endDate || 'all'}.xlsx`;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          window.URL.revokeObjectURL(url);
          
          this.notificationService.showSuccess('Xuất danh sách ca làm việc thành công');
        },
        error: (error) => {
          console.error('Error exporting shifts:', error);
          this.notificationService.showError('Lỗi khi xuất danh sách ca làm việc');
        }
      });
  }

  // Quick date filters
  setToday(): void {
    const today = new Date().toISOString().split('T')[0];
    this.startDate = today;
    this.endDate = today;
    this.onFilterChange();
  }

  setThisWeek(): void {
    const today = new Date();
    const monday = new Date(today);
    monday.setDate(today.getDate() - today.getDay() + 1);
    
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);

    this.startDate = monday.toISOString().split('T')[0];
    this.endDate = sunday.toISOString().split('T')[0];
    this.onFilterChange();
  }

  setThisMonth(): void {
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    this.startDate = firstDay.toISOString().split('T')[0];
    this.endDate = lastDay.toISOString().split('T')[0];
    this.onFilterChange();
  }

  clearDateFilters(): void {
    this.startDate = '';
    this.endDate = '';
    this.onFilterChange();
  }

  // Refresh data
  refresh(): void {
    this.loadWorkShifts();
    this.loadWorkLocations();
    this.loadEmployees();
  }

  // ✅ FIX: Track by function return type should be number (ID type)
  trackByShiftId(index: number, shift: WorkShift): number {
    return shift.id;
  }
}
