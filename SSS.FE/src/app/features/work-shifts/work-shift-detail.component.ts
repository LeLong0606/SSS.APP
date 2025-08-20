import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { WorkShiftService } from '../../core/services/work-shift.service';
import { EmployeeService } from '../../core/services/employee.service';
import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { WorkShift } from '../../core/models/work-shift.model';
import { Employee } from '../../core/models/employee.model';
import { WorkLocation } from '../../core/models/work-location.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-work-shift-detail',
  templateUrl: './work-shift-detail.component.html',
  styleUrls: ['./work-shift-detail.component.scss'],
  standalone: false
})
export class WorkShiftDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  workShiftId: number = 0;
  workShift: WorkShift | null = null;
  employee: Employee | null = null;
  workLocation: WorkLocation | null = null;
  conflictingShifts: WorkShift[] = [];
  
  isLoading = false;
  isLoadingConflicts = false;
  
  // Permissions
  canEdit = false;
  canDelete = false;
  canManageAll = false;

  // Tabs
  activeTab = 'overview';

  constructor(
    private workShiftService: WorkShiftService,
    private employeeService: EmployeeService,
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.initializeRoute();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canManageAll = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private initializeRoute(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params['id'];
      if (id) {
        this.workShiftId = +id;
        this.loadWorkShift();
      } else {
        this.notificationService.showError('ID ca làm việc không hợp lệ');
        this.router.navigate(['/work-shifts']);
      }
    });
  }

  private loadWorkShift(): void {
    this.isLoading = true;
    
    this.workShiftService.getWorkShift(this.workShiftId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success && response.data) {
            this.workShift = response.data;
            this.loadEmployeeDetails();
            this.loadWorkLocationDetails();
            this.loadConflictingShifts();
          } else {
            this.notificationService.showError(response.message || 'Không thể tải thông tin ca làm việc');
            this.router.navigate(['/work-shifts']);
          }
        },
        error: (error: any) => {
          console.error('Error loading work shift:', error);
          this.notificationService.showError('Lỗi khi tải thông tin ca làm việc');
          this.router.navigate(['/work-shifts']);
        },
        complete: () => {
          this.isLoading = false;
        }
      });
  }

  private loadEmployeeDetails(): void {
    if (!this.workShift?.employeeCode) return;

    this.employeeService.getEmployeeByCode(this.workShift.employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success && response.data) {
            this.employee = response.data;
          }
        },
        error: (error: any) => {
          console.error('Error loading employee details:', error);
        }
      });
  }

  private loadWorkLocationDetails(): void {
    if (!this.workShift?.workLocationId) return;

    this.workLocationService.getWorkLocation(this.workShift.workLocationId.toString())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success && response.data) {
            this.workLocation = response.data;
          }
        },
        error: (error: any) => {
          console.error('Error loading work location details:', error);
        }
      });
  }

  private loadConflictingShifts(): void {
    if (!this.workShift) return;

    this.isLoadingConflicts = true;
    this.workShiftService.getShiftConflicts(
      this.workShift.employeeCode,
      this.formatDateForAPI(this.workShift.shiftDate),
      this.workShift.startTime,
      this.workShift.endTime
    ).pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response: any) => {
        if (response.success && response.data) {
          // Filter out current shift from conflicts
          this.conflictingShifts = response.data.filter((shift: WorkShift) => shift.id !== this.workShift?.id);
        }
      },
      error: (error: any) => {
        console.error('Error loading conflicting shifts:', error);
      },
      complete: () => {
        this.isLoadingConflicts = false;
      }
    });
  }

  // Navigation methods
  onEdit(): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa ca làm việc');
      return;
    }

    // Check if user can edit this specific shift
    if (!this.canManageAll && !this.canUserEditShift()) {
      this.notificationService.showError('Bạn chỉ có thể chỉnh sửa ca làm việc của mình');
      return;
    }

    this.router.navigate(['/work-shifts', this.workShiftId, 'edit']);
  }

  onDelete(): void {
    if (!this.canDelete) {
      this.notificationService.showError('Bạn không có quyền xóa ca làm việc');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa ca làm việc này?`)) {
      this.isLoading = true;
      
      this.workShiftService.deleteWorkShift(this.workShiftId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa ca làm việc thành công');
              this.router.navigate(['/work-shifts']);
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa ca làm việc');
            }
          },
          error: (error: any) => {
            console.error('Error deleting work shift:', error);
            this.notificationService.showError('Lỗi khi xóa ca làm việc');
          },
          complete: () => {
            this.isLoading = false;
          }
        });
    }
  }

  onBackToList(): void {
    this.router.navigate(['/work-shifts']);
  }

  // Tab management
  setActiveTab(tab: string): void {
    this.activeTab = tab;
  }

  // Utility methods
  private canUserEditShift(): boolean {
    const currentUser = this.authService.getCurrentUserSync();
    if (!currentUser || !this.workShift) return false;
    
    // Users can edit their own shifts or shifts they created
    return this.workShift.employeeCode === currentUser.employeeCode || 
           this.workShift.createdByEmployeeCode === currentUser.employeeCode;
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN');
  }

  formatDateTime(date: Date | string | undefined): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleString('vi-VN');
  }

  formatTime(time: string): string {
    if (!time) return '';
    return time.substring(0, 5); // HH:mm format
  }

  private formatDateForAPI(date: Date | string): string {
    if (date instanceof Date) {
      return date.toISOString().split('T')[0];
    }
    return date.toString().split('T')[0];
  }

  calculateDuration(): string {
    if (!this.workShift) return '';
    
    try {
      const duration = this.workShiftService.calculateShiftDuration(
        this.workShift.startTime, 
        this.workShift.endTime
      );
      return `${duration} giờ`;
    } catch (error) {
      return '';
    }
  }

  getShiftStatus(): string {
    if (!this.workShift) return '';

    const shiftDate = new Date(this.workShift.shiftDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    shiftDate.setHours(0, 0, 0, 0);

    if (shiftDate > today) return 'Sắp tới';
    if (shiftDate < today) return 'Đã qua';
    return 'Hôm nay';
  }

  getShiftStatusClass(): string {
    const status = this.getShiftStatus();
    switch (status) {
      case 'Sắp tới': return 'status-upcoming';
      case 'Đã qua': return 'status-past';
      case 'Hôm nay': return 'status-today';
      default: return '';
    }
  }

  // Navigate to related entities
  viewEmployee(): void {
    if (this.employee) {
      this.router.navigate(['/employees', this.employee.id]);
    }
  }

  viewWorkLocation(): void {
    if (this.workLocation) {
      this.router.navigate(['/work-locations', this.workLocation.id]);
    }
  }

  viewConflictingShift(shift: WorkShift): void {
    this.router.navigate(['/work-shifts', shift.id]);
  }

  // Refresh data
  refresh(): void {
    this.loadWorkShift();
  }

  // Get formatted address
  getFullAddress(): string {
    if (!this.workLocation) return '';
    
    return this.workLocation.address || '';
  }

  // Track by functions
  trackByShiftId(index: number, shift: WorkShift): number {
    return shift.id;
  }
}
