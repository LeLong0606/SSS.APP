import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';

import { WorkShiftService } from '../../core/services/work-shift.service';
import { EmployeeService } from '../../core/services/employee.service';
import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { WorkShift, CreateWorkShiftRequest, UpdateWorkShiftRequest } from '../../core/models/work-shift.model';
import { Employee } from '../../core/models/employee.model';
import { WorkLocation } from '../../core/models/work-location.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-work-shift-form',
  templateUrl: './work-shift-form.component.html',
  styleUrls: ['./work-shift-form.component.scss'],
  standalone: false
})
export class WorkShiftFormComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  workShiftForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  isSubmitting = false;
  
  workShift: WorkShift | null = null;
  employees: Employee[] = [];
  workLocations: WorkLocation[] = [];
  
  // Permissions
  canEdit = false;
  canCreate = false;
  canManageAll = false;

  // Form validation messages
  validationMessages = {
    employeeCode: {
      required: 'Nhân viên là bắt buộc'
    },
    workLocationId: {
      required: 'Địa điểm làm việc là bắt buộc'
    },
    shiftDate: {
      required: 'Ngày làm việc là bắt buộc'
    },
    startTime: {
      required: 'Giờ bắt đầu là bắt buộc'
    },
    endTime: {
      required: 'Giờ kết thúc là bắt buộc'
    },
    modificationReason: {
      required: 'Lý do thay đổi là bắt buộc khi chỉnh sửa'
    }
  };

  // Quick time presets
  timePresets = [
    { label: 'Ca sáng (7:30 - 15:30)', start: '07:30', end: '15:30' },
    { label: 'Ca chiều (14:00 - 22:00)', start: '14:00', end: '22:00' },
    { label: 'Ca tối (21:00 - 05:00)', start: '21:00', end: '05:00' },
    { label: 'Ca đêm (22:00 - 06:00)', start: '22:00', end: '06:00' },
    { label: 'Ca cả ngày (8:00 - 17:00)', start: '08:00', end: '17:00' }
  ];

  constructor(
    private formBuilder: FormBuilder,
    private workShiftService: WorkShiftService,
    private employeeService: EmployeeService,
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.workShiftForm = this.formBuilder.group({
      employeeCode: ['', [Validators.required]],
      workLocationId: ['', [Validators.required]],
      shiftDate: ['', [Validators.required]],
      startTime: ['', [Validators.required]],
      endTime: ['', [Validators.required]],
      modificationReason: ['']
    });
  }

  ngOnInit(): void {
    this.initializePermissions();
    this.loadData();
    this.initializeForm();
    this.setupFormValidation();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canManageAll = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private loadData(): void {
    this.isLoading = true;

    const calls = {
      employees: this.employeeService.getEmployees({ pageNumber: 1, pageSize: 100 }),
      workLocations: this.workLocationService.getAllWorkLocations()
    };

    forkJoin(calls).subscribe({
      next: (response) => {
        if (response.employees.success && response.employees.data) {
          this.employees = response.employees.data.filter((emp: Employee) => emp.isActive);
        }

        if (response.workLocations.success && response.workLocations.data) {
          this.workLocations = response.workLocations.data.filter(loc => loc.isActive);
        }
      },
      error: (error) => {
        console.error('Error loading data:', error);
        this.notificationService.showError('Lỗi khi tải dữ liệu');
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  private initializeForm(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params['id'];
      if (id && id !== 'create') {
        const workShiftId = +id;
        this.isEditMode = true;
        this.loadWorkShift(workShiftId);
        
        // Add modification reason as required for edit mode
        this.workShiftForm.get('modificationReason')?.setValidators([Validators.required]);
      } else {
        this.isEditMode = false;
        this.setDefaultValues();
      }
    });
  }

  private loadWorkShift(id: number): void {
    this.isLoading = true;
    
    this.workShiftService.getWorkShift(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success && response.data) {
            this.workShift = response.data;
            if (this.workShift) {
              this.populateForm(this.workShift);
            }
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

  private populateForm(workShift: WorkShift): void {
    const shiftDate = workShift.shiftDate instanceof Date 
      ? workShift.shiftDate.toISOString().split('T')[0]
      : String(workShift.shiftDate).split('T')[0];

    this.workShiftForm.patchValue({
      employeeCode: workShift.employeeCode,
      workLocationId: workShift.workLocationId,
      shiftDate: shiftDate,
      startTime: workShift.startTime,
      endTime: workShift.endTime,
      modificationReason: ''
    });
  }

  private setDefaultValues(): void {
    // Set default date to today
    const today = new Date().toISOString().split('T')[0];
    this.workShiftForm.patchValue({
      shiftDate: today
    });

    // Set default employee to current user if they are an employee
    const currentUser = this.authService.getCurrentUserSync();
    if (currentUser && !this.canManageAll) {
      this.workShiftForm.patchValue({
        employeeCode: currentUser.employeeCode
      });
      // Disable employee selection for non-admin users
      this.workShiftForm.get('employeeCode')?.disable();
    }
  }

  private setupFormValidation(): void {
    // Add custom validation for time consistency
    this.workShiftForm.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.validateTimeRange();
    });
  }

  private validateTimeRange(): void {
    const startTime = this.workShiftForm.get('startTime')?.value;
    const endTime = this.workShiftForm.get('endTime')?.value;

    if (startTime && endTime) {
      const start = this.timeToMinutes(startTime);
      const end = this.timeToMinutes(endTime);

      if (end <= start) {
        this.workShiftForm.get('endTime')?.setErrors({ invalidTimeRange: true });
      } else {
        const endTimeControl = this.workShiftForm.get('endTime');
        if (endTimeControl?.errors?.['invalidTimeRange']) {
          delete endTimeControl.errors['invalidTimeRange'];
          if (Object.keys(endTimeControl.errors).length === 0) {
            endTimeControl.setErrors(null);
          }
        }
      }
    }
  }

  private timeToMinutes(time: string): number {
    const [hours, minutes] = time.split(':').map(Number);
    return hours * 60 + minutes;
  }

  onSubmit(): void {
    if (this.workShiftForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.isSubmitting = true;
    const formData = this.workShiftForm.getRawValue(); // Use getRawValue to include disabled fields

    if (this.isEditMode && this.workShift) {
      this.updateWorkShift(formData);
    } else {
      this.createWorkShift(formData);
    }
  }

  private createWorkShift(data: any): void {
    const createRequest: CreateWorkShiftRequest = {
      employeeCode: data.employeeCode,
      workLocationId: Number(data.workLocationId),
      shiftDate: data.shiftDate,
      startTime: data.startTime,
      endTime: data.endTime
    };

    // Validate before submission
    const validationErrors = this.workShiftService.validateShift(createRequest);
    if (validationErrors.length > 0) {
      this.notificationService.showError(validationErrors.join(', '));
      this.isSubmitting = false;
      return;
    }

    this.workShiftService.createWorkShift(createRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Tạo ca làm việc thành công');
            this.router.navigate(['/work-shifts']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi tạo ca làm việc');
          }
        },
        error: (error) => {
          console.error('Error creating work shift:', error);
          this.notificationService.showError('Lỗi khi tạo ca làm việc');
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
  }

  private updateWorkShift(data: any): void {
    if (!this.workShift) return;

    const updateRequest: UpdateWorkShiftRequest = {
      workLocationId: Number(data.workLocationId),
      startTime: data.startTime,
      endTime: data.endTime,
      modificationReason: data.modificationReason
    };

    this.workShiftService.updateWorkShift(this.workShift.id, updateRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Cập nhật ca làm việc thành công');
            this.router.navigate(['/work-shifts']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi cập nhật ca làm việc');
          }
        },
        error: (error) => {
          console.error('Error updating work shift:', error);
          this.notificationService.showError('Lỗi khi cập nhật ca làm việc');
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
  }

  onCancel(): void {
    this.router.navigate(['/work-shifts']);
  }

  // Quick time preset selection
  applyTimePreset(preset: any): void {
    this.workShiftForm.patchValue({
      startTime: preset.start,
      endTime: preset.end
    });
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.workShiftForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched) : false;
  }

  getFieldError(fieldName: string): string {
    const field = this.workShiftForm.get(fieldName);
    if (field && field.errors) {
      if (field.errors['required']) {
        const fieldMessages = this.validationMessages[fieldName as keyof typeof this.validationMessages];
        return fieldMessages?.required || 'Trường này là bắt buộc';
      }
      if (field.errors['invalidTimeRange']) {
        return 'Giờ kết thúc phải sau giờ bắt đầu';
      }
    }
    return '';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.workShiftForm.controls).forEach(key => {
      const control = this.workShiftForm.get(key);
      control?.markAsTouched();
    });
  }

  // Calculate shift duration
  getShiftDuration(): string {
    const startTime = this.workShiftForm.get('startTime')?.value;
    const endTime = this.workShiftForm.get('endTime')?.value;

    if (startTime && endTime) {
      try {
        const duration = this.workShiftService.calculateShiftDuration(startTime, endTime);
        return `${duration} giờ`;
      } catch (error) {
        return '';
      }
    }
    return '';
  }

  // Check for conflicts
  checkShiftConflicts(): void {
    const formValue = this.workShiftForm.value;
    if (formValue.employeeCode && formValue.shiftDate && formValue.startTime && formValue.endTime) {
      this.workShiftService.getShiftConflicts(
        formValue.employeeCode,
        formValue.shiftDate,
        formValue.startTime,
        formValue.endTime
      ).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data && response.data.length > 0) {
            this.notificationService.showWarning(`Có ${response.data.length} ca làm việc bị trung lịch`);
          }
        },
        error: (error) => {
          console.error('Error checking conflicts:', error);
        }
      });
    }
  }

  // Get employee name for display
  getEmployeeName(employeeCode: string): string {
    const employee = this.employees.find(emp => emp.employeeCode === employeeCode);
    return employee ? employee.fullName : employeeCode;
  }

  // Get work location name for display
  getWorkLocationName(locationId: number): string {
    const location = this.workLocations.find(loc => loc.id === locationId);
    return location ? location.name : 'Không xác định';
  }
}
