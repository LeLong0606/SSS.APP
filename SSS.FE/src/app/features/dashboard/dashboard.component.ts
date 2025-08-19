import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable, BehaviorSubject, Subject, takeUntil, combineLatest } from 'rxjs';

import { 
  AuthService,
  EmployeeService,
  DepartmentService,
  WorkShiftService,
  AttendanceService,
  ImageService,
  LoadingService,
  NotificationService
} from '../../core/services';

import { UserInfo, UserRole } from '../../core/models/auth.model';
import { Employee } from '../../core/models/employee.model';
import { Department } from '../../core/models/department.model';
import { WorkShift } from '../../core/models/work-shift.model';
import { AttendanceStatus, CheckInRequest, CheckOutRequest } from '../../core/services/attendance.service';

interface DashboardStats {
  totalEmployees: number;
  totalDepartments: number;
  todayShifts: number;
  pendingLeaveRequests: number;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Observables
  loading$ = this.loadingService.loading$;
  currentUser: UserInfo | null = null;
  
  // Dashboard Data
  dashboardStats: DashboardStats = {
    totalEmployees: 0,
    totalDepartments: 0,
    todayShifts: 0,
    pendingLeaveRequests: 0
  };

  employees: Employee[] = [];
  departments: Department[] = [];
  workShifts: WorkShift[] = [];
  attendanceStatus: AttendanceStatus | null = null;

  // Forms
  employeeForm!: FormGroup;
  workShiftForm!: FormGroup;
  leaveRequestForm!: FormGroup;

  // Image handling
  private imageErrorsMap = new Map<string, boolean>();
  private readonly defaultAvatarUrl = 'assets/images/default-avatar.svg';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private workShiftService: WorkShiftService,
    private attendanceService: AttendanceService,
    private imageService: ImageService,
    private loadingService: LoadingService,
    private notificationService: NotificationService
  ) {
    this.initializeForms();
  }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    this.employeeForm = this.fb.group({
      employeeCode: ['', [Validators.required]],
      fullName: ['', [Validators.required]],
      position: ['', [Validators.required]],
      departmentId: ['', [Validators.required]]
    });

    this.workShiftForm = this.fb.group({
      employeeCode: ['', [Validators.required]],
      shiftDate: ['', [Validators.required]],
      startTime: ['', [Validators.required]],
      endTime: ['', [Validators.required]],
      workLocationId: ['', [Validators.required]]
    });

    this.leaveRequestForm = this.fb.group({
      leaveType: ['ANNUAL_LEAVE', [Validators.required]],
      startDate: ['', [Validators.required]],
      endDate: ['', [Validators.required]],
      reason: ['', [Validators.required]]
    });
  }

  private loadCurrentUser(): void {
    this.authService.authState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(authState => {
        this.currentUser = authState.user;
      });
  }

  private loadDashboardData(): void {
    // Load employees
    this.employeeService.getEmployees({ pageNumber: 1, pageSize: 20 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.employees = response.data || [];
            this.dashboardStats.totalEmployees = response.totalCount || 0;
          }
        },
        error: (error) => {
          console.error('Error loading employees:', error);
        }
      });

    // Load departments
    this.departmentService.getDepartments({ pageNumber: 1, pageSize: 20 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.departments = response.data || [];
            this.dashboardStats.totalDepartments = response.totalCount || 0;
          }
        },
        error: (error) => {
          console.error('Error loading departments:', error);
        }
      });

    // Load work shifts
    this.workShiftService.getWorkShifts(1, 20, undefined, new Date().toISOString().split('T')[0])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.workShifts = response.data || [];
            this.dashboardStats.todayShifts = response.totalCount || 0;
          }
        },
        error: (error) => {
          console.error('Error loading work shifts:', error);
        }
      });

    // Load attendance status if user is available
    if (this.currentUser?.employeeCode) {
      this.loadAttendanceStatus();
    }
  }

  private loadAttendanceStatus(): void {
    if (!this.currentUser?.employeeCode) return;

    this.attendanceService.getCurrentStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success) {
            this.attendanceStatus = response.data;
          }
        },
        error: (error: any) => {
          console.error('Error loading attendance status:', error);
        }
      });
  }

  // Authentication methods
  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  hasRole(role: string): boolean {
    const userRoles = Object.values(UserRole);
    const roleEnum = userRoles.find(r => r.toString() === role);
    return roleEnum ? this.authService.hasRole(roleEnum) : false;
  }

  logout(): void {
    this.authService.logout()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Đăng xuất thành công');
        },
        error: (error) => {
          console.error('Logout error:', error);
          this.notificationService.showError('Có lỗi khi đăng xuất');
        }
      });
  }

  // Form submission methods
  createEmployee(): void {
    if (this.employeeForm.valid) {
      const employeeData = this.employeeForm.value;
      
      this.employeeService.createEmployee(employeeData)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Tạo nhân viên thành công');
              this.employeeForm.reset();
              this.loadDashboardData();
            } else {
              this.notificationService.showError(response.message || 'Có lỗi xảy ra');
            }
          },
          error: (error) => {
            console.error('Error creating employee:', error);
            this.notificationService.showError('Lỗi tạo nhân viên');
          }
        });
    }
  }

  createWorkShift(): void {
    if (this.workShiftForm.valid) {
      const shiftData = this.workShiftForm.value;
      
      this.workShiftService.createWorkShift(shiftData)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Tạo ca làm việc thành công');
              this.workShiftForm.reset();
              this.loadDashboardData();
            } else {
              this.notificationService.showError(response.message || 'Có lỗi xảy ra');
            }
          },
          error: (error) => {
            console.error('Error creating work shift:', error);
            this.notificationService.showError('Lỗi tạo ca làm việc');
          }
        });
    }
  }

  createLeaveRequest(): void {
    if (this.leaveRequestForm.valid) {
      const leaveData = this.leaveRequestForm.value;
      
      // Assuming we have a leave request service
      console.log('Creating leave request:', leaveData);
      this.notificationService.showInfo('Tính năng nghỉ phép sẽ được cập nhật');
      this.leaveRequestForm.reset();
    }
  }

  // Attendance methods
  checkIn(): void {
    if (!this.currentUser?.employeeCode) return;

    const checkInRequest: CheckInRequest = {
      checkInTime: new Date(),
      notes: 'Dashboard check-in'
    };

    this.attendanceService.checkIn(checkInRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success) {
            this.notificationService.showSuccess('Check-in thành công');
            this.loadAttendanceStatus();
          } else {
            this.notificationService.showError(response.message || 'Lỗi check-in');
          }
        },
        error: (error: any) => {
          console.error('Check-in error:', error);
          this.notificationService.showError('Lỗi check-in');
        }
      });
  }

  checkOut(): void {
    if (!this.currentUser?.employeeCode) return;

    const checkOutRequest: CheckOutRequest = {
      checkOutTime: new Date(),
      notes: 'Dashboard check-out'
    };

    this.attendanceService.checkOut(checkOutRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success) {
            this.notificationService.showSuccess('Check-out thành công');
            this.loadAttendanceStatus();
          } else {
            this.notificationService.showError(response.message || 'Lỗi check-out');
          }
        },
        error: (error: any) => {
          console.error('Check-out error:', error);
          this.notificationService.showError('Lỗi check-out');
        }
      });
  }

  // Image handling methods - Improved to avoid infinite reload
  onImageError(event: Event): void {
    const target = event.target as HTMLImageElement;
    if (target) {
      const currentSrc = target.src;
      
      // If already showing default avatar, don't change anything to avoid infinite loop
      if (currentSrc.includes(this.defaultAvatarUrl)) {
        console.warn('Default avatar image also failed to load');
        return;
      }

      // Mark this URL as failed and switch to default
      this.imageErrorsMap.set(currentSrc, true);
      target.src = this.defaultAvatarUrl;
    }
  }

  onEmployeePhotoChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    const files = target?.files;
    if (files && files.length > 0) {
      this.uploadEmployeePhoto(files[0]);
    }
  }

  uploadEmployeePhoto(file: File): void {
    if (!this.currentUser?.employeeCode || !file) return;

    this.imageService.setEmployeePhoto(this.currentUser.employeeCode, file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (response.success) {
            this.notificationService.showSuccess('Cập nhật ảnh thành công');
            // Clear the error cache for this user to allow reloading
            if (this.currentUser?.employeeCode) {
              const photoUrl = this.imageService.getEmployeePhoto(this.currentUser.employeeCode);
              this.imageErrorsMap.delete(photoUrl);
            }
          } else {
            this.notificationService.showError(response.message || 'Lỗi upload ảnh');
          }
        },
        error: (error: any) => {
          console.error('Error uploading photo:', error);
          this.notificationService.showError('Lỗi upload ảnh');
        }
      });
  }

  onFileSelected(event: Event, fileType: string): void {
    const target = event.target as HTMLInputElement;
    const files = target?.files;
    if (files && files.length > 0) {
      this.uploadFile(files[0], fileType);
    }
  }

  private uploadFile(file: File, fileType: string): void {
    this.imageService.uploadImage(file, fileType)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Upload file thành công');
          } else {
            this.notificationService.showError(response.message || 'Lỗi upload file');
          }
        },
        error: (error) => {
          console.error('Error uploading file:', error);
          this.notificationService.showError('Lỗi upload file');
        }
      });
  }

  // Utility methods
  getEmployeePhotoUrl(employeeCode: string | undefined): string {
    if (!employeeCode) return this.defaultAvatarUrl;
    
    const photoUrl = this.imageService.getEmployeePhoto(employeeCode);
    
    // If this URL has failed before, return default avatar immediately
    if (this.imageErrorsMap.has(photoUrl)) {
      return this.defaultAvatarUrl;
    }
    
    return photoUrl;
  }

  formatTime(time: string | undefined): string {
    if (!time) return '--:--';
    return time;
  }
}
