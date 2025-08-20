import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { DepartmentService } from '../../core/services/department.service';
import { EmployeeService } from '../../core/services/employee.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { Department } from '../../core/models/department.model';
import { Employee } from '../../core/models/employee.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-department-form',
  templateUrl: './department-form.component.html',
  styleUrls: ['./department-form.component.scss'],
  standalone: false
})
export class DepartmentFormComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  @Input() departmentId?: number;
  
  departmentForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  isSubmitting = false;
  
  department: Department | null = null;
  employees: Employee[] = [];
  
  // Permissions
  canEdit = false;
  canCreate = false;

  // Form validation messages
  validationMessages = {
    name: {
      required: 'Tên phòng ban là bắt buộc',
      minlength: 'Tên phòng ban phải có ít nhất 3 ký tự',
      maxlength: 'Tên phòng ban không được quá 100 ký tự'
    },
    departmentCode: {
      required: 'Mã phòng ban là bắt buộc',
      pattern: 'Mã phòng ban chỉ được chứa chữ cái, số và dấu gạch ngang',
      maxlength: 'Mã phòng ban không được quá 20 ký tự'
    },
    description: {
      maxlength: 'Mô tả không được quá 500 ký tự'
    }
  };

  constructor(
    private formBuilder: FormBuilder,
    private departmentService: DepartmentService,
    private employeeService: EmployeeService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.departmentForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      departmentCode: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9\-]+$/), Validators.maxLength(20)]],
      description: ['', [Validators.maxLength(500)]],
      teamLeaderEmployeeCode: [''],
      isActive: [true],
      parentDepartmentId: [null]
    });
  }

  ngOnInit(): void {
    this.initializePermissions();
    this.loadEmployees();
    this.initializeForm();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private initializeForm(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params['id'];
      if (id && id !== 'create') {
        this.departmentId = +id;
        this.isEditMode = true;
        this.loadDepartment();
      } else {
        this.isEditMode = false;
        this.departmentForm.patchValue({
          isActive: true
        });
      }
    });
  }

  private loadDepartment(): void {
    if (!this.departmentId) return;

    this.isLoading = true;
    this.departmentService.getDepartment(this.departmentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success && response.data) {
            this.department = response.data;
            this.departmentForm.patchValue({
              name: this.department?.name || '',
              departmentCode: this.department?.departmentCode || '',
              description: this.department?.description || '',
              teamLeaderEmployeeCode: this.department?.teamLeaderEmployeeCode || '',
              isActive: this.department?.isActive || true,
              parentDepartmentId: null // TODO: Add parentDepartmentId when model supports it
            });
          } else {
            this.notificationService.showError(response.message || 'Không thể tải thông tin phòng ban');
            this.router.navigate(['/departments']);
          }
        },
        error: (error: any) => {
          console.error('Error loading department:', error);
          this.notificationService.showError('Lỗi khi tải thông tin phòng ban');
          this.router.navigate(['/departments']);
        },
        complete: () => {
          this.isLoading = false;
        }
      });
  }

  private loadEmployees(): void {
    this.employeeService.getEmployees({ pageNumber: 1, pageSize: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employees = response.data.filter(emp => emp.isActive);
          }
        },
        error: (error) => {
          console.error('Error loading employees:', error);
        }
      });
  }

  onSubmit(): void {
    if (this.departmentForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.isSubmitting = true;
    const formData = this.departmentForm.value;

    if (this.isEditMode && this.departmentId) {
      this.updateDepartment(formData);
    } else {
      this.createDepartment(formData);
    }
  }

  private createDepartment(data: any): void {
    this.departmentService.createDepartment(data)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Tạo phòng ban thành công');
            this.router.navigate(['/departments']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi tạo phòng ban');
          }
        },
        error: (error) => {
          console.error('Error creating department:', error);
          this.notificationService.showError('Lỗi khi tạo phòng ban');
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
  }

  private updateDepartment(data: any): void {
    if (!this.departmentId) return;

    this.departmentService.updateDepartment(this.departmentId, data)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Cập nhật phòng ban thành công');
            this.router.navigate(['/departments']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi cập nhật phòng ban');
          }
        },
        error: (error) => {
          console.error('Error updating department:', error);
          this.notificationService.showError('Lỗi khi cập nhật phòng ban');
        },
        complete: () => {
          this.isSubmitting = false;
        }
      });
  }

  onCancel(): void {
    this.router.navigate(['/departments']);
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.departmentForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched) : false;
  }

  getFieldError(fieldName: string): string {
    const field = this.departmentForm.get(fieldName);
    if (field && field.errors) {
      const firstError = Object.keys(field.errors)[0];
      const fieldMessages = this.validationMessages[fieldName as keyof typeof this.validationMessages];
      if (fieldMessages && typeof fieldMessages === 'object') {
        return (fieldMessages as any)[firstError] || 'Lỗi không xác định';
      }
    }
    return 'Lỗi không xác định';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.departmentForm.controls).forEach(key => {
      const control = this.departmentForm.get(key);
      control?.markAsTouched();
    });
  }

  // Get available team leaders (employees who are not already team leaders elsewhere)
  getAvailableTeamLeaders(): Employee[] {
    if (!this.departmentId) return this.employees;
    
    // Filter out employees who are already team leaders in other departments
    return this.employees.filter(emp => 
      emp.isActive && 
      (emp.isTeamLeader || emp.employeeCode !== this.department?.teamLeaderEmployeeCode)
    );
  }

  // Check if department code is unique
  checkDepartmentCodeUnique(): void {
    const code = this.departmentForm.get('departmentCode')?.value;
    if (!code || this.isEditMode) return;

    // TODO: Implement department code uniqueness check
    // For now, just log the check
    console.log('Checking department code uniqueness:', code);
  }
}
