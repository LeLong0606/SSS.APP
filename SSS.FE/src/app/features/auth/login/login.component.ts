import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { trigger, transition, style, animate, keyframes } from '@angular/animations';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { LoginRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [
    trigger('slideInUp', [
      transition(':enter', [
        animate('0.6s ease-out', keyframes([
          style({ opacity: 0, transform: 'translateY(100px) scale(0.8)', offset: 0 }),
          style({ opacity: 0.7, transform: 'translateY(-10px) scale(1.05)', offset: 0.7 }),
          style({ opacity: 1, transform: 'translateY(0) scale(1)', offset: 1 })
        ]))
      ])
    ]),
    trigger('fadeInStagger', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(30px)' }),
        animate('0.5s 0.2s ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ]),
    trigger('buttonLoading', [
      transition('idle => loading', [
        animate('0.3s ease-out', style({ transform: 'scale(0.95)' }))
      ]),
      transition('loading => idle', [
        animate('0.3s ease-out', style({ transform: 'scale(1)' }))
      ])
    ])
  ],
  standalone: false
})
export class LoginComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  loginForm!: FormGroup;
  isLoading = false;
  hidePassword = true;
  showWelcomeAnimation = true;
  
  // UI State
  currentYear = new Date().getFullYear();
  loginAttempts = 0;
  maxAttempts = 5;
  
  // Animation states
  buttonState: 'idle' | 'loading' = 'idle';

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.createForm();
    this.checkAlreadyLoggedIn();
    this.setupAnimations();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [
        Validators.required,
        Validators.email,
        Validators.maxLength(100)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(100)
      ]]
    });
  }

  private checkAlreadyLoggedIn(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  private setupAnimations(): void {
    // Hide welcome animation after delay
    setTimeout(() => {
      this.showWelcomeAnimation = false;
    }, 3000);
  }

  onSubmit(): void {
    if (this.loginForm.valid && !this.isLoading) {
      this.performLogin();
    } else {
      this.markFormGroupTouched();
      this.showValidationErrors();
    }
  }

  private performLogin(): void {
    this.isLoading = true;
    this.buttonState = 'loading';
    this.loginAttempts++;

    const loginData: LoginRequest = {
      email: this.loginForm.value.email.trim(),
      password: this.loginForm.value.password
    };

    // Show loading notification
    const loadingId = this.notificationService.showLoading(
      'Đang đăng nhập',
      'Vui lòng chờ trong giây lát...'
    );

    this.authService.login(loginData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.handleLoginSuccess(loadingId, response);
        },
        error: (error) => {
          this.handleLoginError(loadingId, error);
        }
      });
  }

  private handleLoginSuccess(loadingId: string, response: any): void {
    this.isLoading = false;
    this.buttonState = 'idle';

    // Complete loading notification
    this.notificationService.completeLoading(
      loadingId,
      'Đăng nhập thành công!',
      'Chào mừng bạn trở lại hệ thống.'
    );

    // Navigate to dashboard
    setTimeout(() => {
      this.router.navigate(['/dashboard']);
    }, 1000);
  }

  private handleLoginError(loadingId: string, error: any): void {
    this.isLoading = false;
    this.buttonState = 'idle';

    // Hide loading notification
    this.notificationService.hideNotification(loadingId);

    console.error('Login error:', error);

    let errorMessage = 'Đăng nhập thất bại';
    let errorDescription = 'Vui lòng kiểm tra lại thông tin đăng nhập.';

    if (error.error?.message) {
      errorMessage = 'Đăng nhập thất bại';
      errorDescription = error.error.message;
    } else if (error.status === 401) {
      errorMessage = 'Thông tin không hợp lệ';
      errorDescription = 'Email hoặc mật khẩu không đúng.';
    } else if (error.status === 429) {
      errorMessage = 'Quá nhiều lần thử';
      errorDescription = 'Vui lòng thử lại sau 5 phút.';
    } else if (error.status === 0) {
      errorMessage = 'Lỗi kết nối';
      errorDescription = 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.';
    }

    this.notificationService.showError(errorMessage, errorDescription, {
      duration: 8000,
      actions: [
        {
          label: 'Thử lại',
          style: 'primary',
          action: () => this.onSubmit()
        },
        {
          label: 'Quên mật khẩu?',
          style: 'secondary',
          action: () => this.navigateToForgotPassword()
        }
      ]
    });

    // Show account lockout warning
    if (this.loginAttempts >= this.maxAttempts - 1) {
      this.notificationService.showWarning(
        'Cảnh báo bảo mật',
        `Bạn đã thử đăng nhập ${this.loginAttempts}/${this.maxAttempts} lần. Tài khoản có thể bị khóa tạm thời.`
      );
    }

    // Add shake animation to form
    this.shakeForm();
  }

  private shakeForm(): void {
    const formElement = document.querySelector('.login-form') as HTMLElement;
    if (formElement) {
      formElement.classList.add('shake');
      setTimeout(() => {
        formElement.classList.remove('shake');
      }, 600);
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      control?.markAsTouched();
    });
  }

  private showValidationErrors(): void {
    const errors: string[] = [];
    
    if (this.loginForm.get('email')?.errors) {
      errors.push('Email không hợp lệ');
    }
    
    if (this.loginForm.get('password')?.errors) {
      errors.push('Mật khẩu phải có ít nhất 6 ký tự');
    }

    if (errors.length > 0) {
      this.notificationService.showWarning(
        'Thông tin không hợp lệ',
        errors.join(', ')
      );
    }
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.loginForm.get(fieldName);
    
    if (field?.errors) {
      if (field.errors['required']) {
        return this.getRequiredErrorMessage(fieldName);
      }
      if (field.errors['email']) {
        return 'Địa chỉ email không hợp lệ';
      }
      if (field.errors['minlength']) {
        const minLength = field.errors['minlength'].requiredLength;
        return `Mật khẩu phải có ít nhất ${minLength} ký tự`;
      }
      if (field.errors['maxlength']) {
        const maxLength = field.errors['maxlength'].requiredLength;
        return `${this.getFieldLabel(fieldName)} không được quá ${maxLength} ký tự`;
      }
    }
    
    return '';
  }

  private getRequiredErrorMessage(fieldName: string): string {
    return `${this.getFieldLabel(fieldName)} là bắt buộc`;
  }

  private getFieldLabel(fieldName: string): string {
    const fieldLabels: { [key: string]: string } = {
      email: 'Email',
      password: 'Mật khẩu'
    };
    
    return fieldLabels[fieldName] || fieldName;
  }

  // UI helper methods
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
    
    // Add haptic feedback if available
    if ('vibrate' in navigator) {
      navigator.vibrate(50);
    }
  }

  navigateToRegister(): void {
    this.router.navigate(['/auth/register']);
  }

  navigateToForgotPassword(): void {
    this.notificationService.showInfo(
      'Tính năng đang phát triển',
      'Chức năng quên mật khẩu sẽ sớm được cập nhật.'
    );
  }

  // Quick login for development (remove in production)
  quickLogin(role: 'admin' | 'employee'): void {
    if (role === 'admin') {
      this.loginForm.patchValue({
        email: 'admin@sss.com',
        password: 'admin123'
      });
    } else {
      this.loginForm.patchValue({
        email: 'employee@sss.com',
        password: 'employee123'
      });
    }
    
    this.onSubmit();
  }

  // Accessibility helpers
  onEnterKey(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !this.isLoading) {
      this.onSubmit();
    }
  }
}
