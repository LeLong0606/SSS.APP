import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RegisterRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: false
})
export class RegisterComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  registerForm!: FormGroup;
  isLoading = false;
  hidePassword = true;
  hideConfirmPassword = true;

  // ✅ FIX: Available roles as strings (matching backend exactly)
  availableRoles = [
    { value: 'Administrator', label: 'Quản trị viên' },
    { value: 'Director', label: 'Giám đốc' },
    { value: 'TeamLeader', label: 'Trưởng phòng' },
    { value: 'Employee', label: 'Nhân viên' }
  ];

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.createForm();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): void {
    this.registerForm = this.formBuilder.group({
      email: ['', [
        Validators.required,
        Validators.email,
        Validators.maxLength(100)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(100)
      ]],
      confirmPassword: ['', [
        Validators.required
      ]],
      fullName: ['', [
        Validators.required,
        Validators.maxLength(200)
      ]],
      employeeCode: ['', [
        Validators.maxLength(50)
      ]],
      role: ['Employee', [
        Validators.required
      ]],
      acceptTerms: [false, [
        Validators.requiredTrue
      ]]
    }, {
      validators: [this.passwordMatchValidator]
    });
  }

  // Custom validator for password confirmation
  private passwordMatchValidator(control: AbstractControl): { [key: string]: boolean } | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }
    
    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  // ✅ FIXED: Send role as string to match backend exactly
  onSubmit(): void {
    if (this.registerForm.valid) {
      this.isLoading = true;
      
      const formValue = this.registerForm.value;
      
      // ✅ FIX: Create request that EXACTLY matches backend RegisterRequest
      const registerData: RegisterRequest = {
        email: formValue.email.trim(),
        password: formValue.password,
        confirmPassword: formValue.confirmPassword,
        fullName: formValue.fullName.trim(),
        employeeCode: formValue.employeeCode ? formValue.employeeCode.trim() : undefined,
        role: formValue.role // String value, not enum - CRITICAL FIX!
      };

      this.authService.register(registerData)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            this.isLoading = false;
            
            if (response.success) {
              this.notificationService.showSuccess('Đăng ký thành công! Vui lòng đăng nhập.');
              this.router.navigate(['/auth/login']);
            } else {
              this.handleRegistrationError(response.message || 'Đăng ký thất bại', response.errors);
            }
          },
          error: (error) => {
            this.isLoading = false;
            console.error('Registration error:', error);
            this.handleRegistrationError(
              'Đăng ký thất bại. Vui lòng thử lại.',
              error.error?.errors || []
            );
          }
        });
    } else {
      this.markFormGroupTouched();
    }
  }

  private handleRegistrationError(message: string, errors: string[] = []): void {
    if (errors && errors.length > 0) {
      // Show first error or combine multiple errors
      const errorMessage = errors.length === 1 
        ? errors[0]
        : `${message}\n• ${errors.join('\n• ')}`;
      
      this.notificationService.showError(errorMessage);
    } else {
      this.notificationService.showError(message);
    }
    
    // Handle specific validation errors
    if (errors.some(err => err.toLowerCase().includes('email'))) {
      this.registerForm.get('email')?.setErrors({ serverError: true });
    }
    
    if (errors.some(err => err.toLowerCase().includes('employeecode'))) {
      this.registerForm.get('employeeCode')?.setErrors({ serverError: true });
    }

    if (errors.some(err => err.toLowerCase().includes('role'))) {
      this.registerForm.get('role')?.setErrors({ serverError: true });
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      control?.markAsTouched();
    });
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.registerForm.get(fieldName);
    
    if (field?.errors) {
      if (field.errors['required']) {
        return this.getRequiredErrorMessage(fieldName);
      }
      if (field.errors['email']) {
        return 'Địa chỉ email không hợp lệ';
      }
      if (field.errors['minlength']) {
        const minLength = field.errors['minlength'].requiredLength;
        return `${this.getFieldLabel(fieldName)} phải có ít nhất ${minLength} ký tự`;
      }
      if (field.errors['maxlength']) {
        const maxLength = field.errors['maxlength'].requiredLength;
        return `${this.getFieldLabel(fieldName)} không được quá ${maxLength} ký tự`;
      }
      if (field.errors['requiredTrue']) {
        return 'Bạn phải đồng ý với các điều khoản';
      }
      if (field.errors['serverError']) {
        return 'Có lỗi xảy ra với trường này';
      }
    }
    
    // Check form-level errors
    if (fieldName === 'confirmPassword' && this.registerForm.errors?.['passwordMismatch']) {
      return 'Mật khẩu xác nhận không khớp';
    }
    
    return '';
  }

  private getRequiredErrorMessage(fieldName: string): string {
    return `${this.getFieldLabel(fieldName)} là bắt buộc`;
  }

  private getFieldLabel(fieldName: string): string {
    const fieldLabels: { [key: string]: string } = {
      email: 'Email',
      password: 'Mật khẩu',
      confirmPassword: 'Xác nhận mật khẩu',
      fullName: 'Họ tên',
      employeeCode: 'Mã nhân viên',
      role: 'Vai trò',
      acceptTerms: 'Điều khoản'
    };
    
    return fieldLabels[fieldName] || fieldName;
  }

  // UI helper methods
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword = !this.hideConfirmPassword;
  }

  navigateToLogin(): void {
    this.router.navigate(['/auth/login']);
  }
}
