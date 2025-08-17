import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, finalize } from 'rxjs';

import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { Employee, CreateEmployeeRequest, UpdateEmployeeRequest } from '../../core/models/employee.model';
import { Department } from '../../core/models/department.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-employee-form',
  templateUrl: './employee-form.component.html',
  styleUrls: ['./employee-form.component.scss'],
  standalone: false
})
export class EmployeeFormComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  employeeForm: FormGroup;
  departments: Department[] = [];
  isLoading = false;
  isSubmitting = false;
  isEditMode = false;
  employeeId: number | null = null;
  currentEmployee: Employee | null = null;

  // Permissions
  canCreate = false;
  canEdit = false;
  canAssignTeamLeader = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private notificationService: NotificationService,
    private authService: AuthService
  ) {
    this.employeeForm = this.createForm();
  }

  ngOnInit(): void {
    this.initializePermissions();
    this.loadDepartments();
    this.checkEditMode();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canAssignTeamLeader = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private createForm(): FormGroup {
    return this.fb.group({
      employeeCode: ['', [Validators.required, Validators.maxLength(50)]],
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      position: ['', [Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.maxLength(20)]],
      address: ['', [Validators.maxLength(200)]],
      hireDate: [''],
      salary: ['', [Validators.min(0)]],
      departmentId: [''],
      isTeamLeader: [false],
      isActive: [true]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.employeeId = parseInt(id, 10);
      this.isEditMode = true;
      this.loadEmployee(this.employeeId);
      
      // Disable employeeCode in edit mode
      this.employeeForm.get('employeeCode')?.disable();
    }
  }

  private loadDepartments(): void {
    this.departmentService.getAllDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.departments = response.data.filter(dept => dept.isActive);
          }
        },
        error: (error) => {
          console.error('Error loading departments:', error);
          this.notificationService.showError('Lỗi khi tải danh sách phòng ban');
        }
      });
  }

  private loadEmployee(id: number): void {
    this.isLoading = true;
    
    this.employeeService.getEmployee(id)
      .pipe(
        finalize(() => this.isLoading = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.currentEmployee = response.data;
            this.populateForm(response.data);
          } else {
            this.notificationService.showError('Không tìm thấy nhân viên');
            this.router.navigate(['/employees']);
          }
        },
        error: (error) => {
          console.error('Error loading employee:', error);
          this.notificationService.showError('Lỗi khi tải thông tin nhân viên');
          this.router.navigate(['/employees']);
        }
      });
  }

  private populateForm(employee: Employee): void {
    this.employeeForm.patchValue({
      employeeCode: employee.employeeCode,
      fullName: employee.fullName,
      position: employee.position,
      phoneNumber: employee.phoneNumber,
      address: employee.address,
      hireDate: employee.hireDate ? new Date(employee.hireDate).toISOString().split('T')[0] : '',
      salary: employee.salary,
      departmentId: employee.departmentId || '',
      isTeamLeader: employee.isTeamLeader,
      isActive: employee.isActive
    });
  }

  onSubmit(): void {
    if (this.employeeForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    if (this.isEditMode && !this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa nhân viên');
      return;
    }

    if (!this.isEditMode && !this.canCreate) {
      this.notificationService.showError('Bạn không có quyền tạo nhân viên mới');
      return;
    }

    this.isSubmitting = true;

    if (this.isEditMode) {
      this.updateEmployee();
    } else {
      this.createEmployee();
    }
  }

  private createEmployee(): void {
    const formValue = this.employeeForm.value;
    const request: CreateEmployeeRequest = {
      employeeCode: formValue.employeeCode,
      fullName: formValue.fullName,
      position: formValue.position || undefined,
      phoneNumber: formValue.phoneNumber || undefined,
      address: formValue.address || undefined,
      hireDate: formValue.hireDate ? new Date(formValue.hireDate) : undefined,
      salary: formValue.salary || undefined,
      departmentId: formValue.departmentId || undefined,
      isTeamLeader: this.canAssignTeamLeader ? formValue.isTeamLeader : false
    };

    this.employeeService.createEmployee(request)
      .pipe(
        finalize(() => this.isSubmitting = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Tạo nhân viên thành công');
            this.router.navigate(['/employees']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi tạo nhân viên');
          }
        },
        error: (error) => {
          console.error('Error creating employee:', error);
          this.handleFormError(error);
        }
      });
  }

  private updateEmployee(): void {
    if (!this.employeeId) return;

    const formValue = this.employeeForm.getRawValue(); // Get raw value to include disabled fields
    const request: UpdateEmployeeRequest = {
      fullName: formValue.fullName,
      position: formValue.position || undefined,
      phoneNumber: formValue.phoneNumber || undefined,
      address: formValue.address || undefined,
      hireDate: formValue.hireDate ? new Date(formValue.hireDate) : undefined,
      salary: formValue.salary || undefined,
      departmentId: formValue.departmentId || undefined,
      isTeamLeader: this.canAssignTeamLeader ? formValue.isTeamLeader : this.currentEmployee?.isTeamLeader,
      isActive: formValue.isActive
    };

    this.employeeService.updateEmployee(this.employeeId, request)
      .pipe(
        finalize(() => this.isSubmitting = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Cập nhật nhân viên thành công');
            this.router.navigate(['/employees']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi cập nhật nhân viên');
          }
        },
        error: (error) => {
          console.error('Error updating employee:', error);
          this.handleFormError(error);
        }
      });
  }

  private handleFormError(error: any): void {
    if (error.status === 400 && error.error?.errors) {
      // Handle validation errors
      const errors = error.error.errors;
      if (Array.isArray(errors)) {
        errors.forEach(errorMsg => {
          this.notificationService.showError(errorMsg);
        });
      }
    } else {
      this.notificationService.showError('Có lỗi xảy ra khi xử lý yêu cầu');
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.employeeForm.controls).forEach(key => {
      const control = this.employeeForm.get(key);
      control?.markAsTouched();
    });
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.employeeForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.employeeForm.get(fieldName);
    
    if (field?.errors) {
      if (field.errors['required']) {
        return this.getRequiredErrorMessage(fieldName);
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldLabel(fieldName)} quá dài`;
      }
      if (field.errors['min']) {
        return `${this.getFieldLabel(fieldName)} phải lớn hơn hoặc bằng 0`;
      }
    }
    
    return '';
  }

  private getRequiredErrorMessage(fieldName: string): string {
    const fieldLabels: { [key: string]: string } = {
      employeeCode: 'Mã nhân viên',
      fullName: 'Họ tên',
      position: 'Chức vụ',
      phoneNumber: 'Số điện thoại',
      address: 'Địa chỉ',
      hireDate: 'Ngày vào làm',
      salary: 'Lương',
      departmentId: 'Phòng ban'
    };
    
    return `${fieldLabels[fieldName] || fieldName} là bắt buộc`;
  }

  private getFieldLabel(fieldName: string): string {
    const fieldLabels: { [key: string]: string } = {
      employeeCode: 'Mã nhân viên',
      fullName: 'Họ tên',
      position: 'Chức vụ',
      phoneNumber: 'Số điện thoại',
      address: 'Địa chỉ',
      hireDate: 'Ngày vào làm',
      salary: 'Lương',
      departmentId: 'Phòng ban'
    };
    
    return fieldLabels[fieldName] || fieldName;
  }

  // Navigation methods
  goBack(): void {
    this.router.navigate(['/employees']);
  }

  cancel(): void {
    if (this.employeeForm.dirty) {
      if (confirm('Bạn có chắc chắn muốn hủy? Các thay đổi sẽ không được lưu.')) {
        this.goBack();
      }
    } else {
      this.goBack();
    }
  }

  // Helper methods for template
  getPageTitle(): string {
    return this.isEditMode ? 'Chỉnh sửa nhân viên' : 'Thêm nhân viên mới';
  }

  getSubmitButtonText(): string {
    return this.isEditMode ? 'Cập nhật' : 'Tạo mới';
  }

  canShowTeamLeaderOption(): boolean {
    return this.canAssignTeamLeader;
  }
}
