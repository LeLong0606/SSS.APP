import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

import { 
  AuthService,
  EmployeeService, 
  DepartmentService,
  WorkShiftService,
  ImageService,
  AttendanceService,
  PayrollService,
  LoadingService
} from '../../core/services';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  // Loading states
  loading$ = this.loadingService.loading$;
  
  // Data
  currentUser: any = null;
  employees: any[] = [];
  departments: any[] = [];
  workShifts: any[] = [];
  attendanceStatus: any = null;
  
  // Forms - Initialize with definite assignment assertion
  employeeForm!: FormGroup;
  workShiftForm!: FormGroup;
  leaveRequestForm!: FormGroup;
  
  // Statistics
  dashboardStats = {
    totalEmployees: 0,
    totalDepartments: 0,
    todayShifts: 0,
    pendingLeaveRequests: 0
  };

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private workShiftService: WorkShiftService,
    private imageService: ImageService,
    private attendanceService: AttendanceService,
    private payrollService: PayrollService,
    private loadingService: LoadingService
  ) {
    this.initializeForms();
  }

  ngOnInit(): void {
    this.loadInitialData();
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
      departmentId: ['', [Validators.required]],
      phoneNumber: [''],
      address: ['']
    });

    this.workShiftForm = this.fb.group({
      employeeCode: ['', [Validators.required]],
      workLocationId: ['', [Validators.required]],
      shiftDate: ['', [Validators.required]],
      startTime: ['', [Validators.required]],
      endTime: ['', [Validators.required]]
    });

    this.leaveRequestForm = this.fb.group({
      startDate: ['', [Validators.required]],
      endDate: ['', [Validators.required]],
      leaveType: ['ANNUAL_LEAVE', [Validators.required]],
      reason: ['', [Validators.required]]
    });
  }

  private loadInitialData(): void {
    // Load current user
    this.authService.getCurrentUser()
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.currentUser = response.user;
        }
      });

    // Load employees
    this.loadEmployees();
    
    // Load departments
    this.loadDepartments();
    
    // Load work shifts
    this.loadWorkShifts();
    
    // Load attendance status
    this.loadAttendanceStatus();
    
    // Load dashboard statistics
    this.loadDashboardStats();
  }

  // Employee Management - FIXED: Use filter object instead of separate arguments
  loadEmployees(): void {
    this.employeeService.getEmployees({ pageNumber: 1, pageSize: 50 })
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.employees = response.data;
          this.dashboardStats.totalEmployees = response.totalCount;
        }
      });
  }

  createEmployee(): void {
    if (this.employeeForm.valid) {
      const employeeData = this.employeeForm.value;
      
      this.employeeService.createEmployee(employeeData)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.employeeForm.reset())
        )
        .subscribe(response => {
          if (response.success) {
            console.log('Employee created successfully!');
            this.loadEmployees(); // Refresh list
          } else {
            console.error('Failed to create employee:', response.errors);
          }
        });
    }
  }

  // Department Management - FIXED: Use filter object instead of separate arguments
  loadDepartments(): void {
    this.departmentService.getDepartments({ pageNumber: 1, pageSize: 20 })
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.departments = response.data;
          this.dashboardStats.totalDepartments = response.totalCount;
        }
      });
  }

  // Work Shift Management - Fixed method calls
  loadWorkShifts(): void {
    const today = new Date().toISOString().split('T')[0];
    
    // Fixed: Use correct parameter names for the service method
    this.workShiftService.getWorkShifts(1, 50, undefined, today, today)
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.workShifts = response.data;
          this.dashboardStats.todayShifts = response.totalCount;
        }
      });
  }

  createWorkShift(): void {
    if (this.workShiftForm.valid) {
      const shiftData = {
        ...this.workShiftForm.value,
        shiftDate: new Date(this.workShiftForm.value.shiftDate),
        workLocationId: Number(this.workShiftForm.value.workLocationId)
      };
      
      this.workShiftService.createWorkShift(shiftData)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.workShiftForm.reset())
        )
        .subscribe(response => {
          if (response.success) {
            console.log('Work shift created successfully!');
            this.loadWorkShifts(); // Refresh list
          } else {
            console.error('Failed to create work shift:', response.errors);
          }
        });
    }
  }

  // Attendance Management
  loadAttendanceStatus(): void {
    this.attendanceService.getCurrentStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.attendanceStatus = response.data;
        }
      });
  }

  checkIn(): void {
    const checkInData = {
      checkInTime: new Date(),
      notes: 'Check-in from dashboard'
    };

    this.attendanceService.checkIn(checkInData)
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          console.log('Checked in successfully!');
          this.loadAttendanceStatus(); // Refresh status
        } else {
          console.error('Failed to check in:', response.errors);
        }
      });
  }

  checkOut(): void {
    const checkOutData = {
      checkOutTime: new Date(),
      notes: 'Check-out from dashboard'
    };

    this.attendanceService.checkOut(checkOutData)
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          console.log('Checked out successfully!');
          this.loadAttendanceStatus(); // Refresh status
        } else {
          console.error('Failed to check out:', response.errors);
        }
      });
  }

  // Image Management
  onFileSelected(event: any, fileType: string): void {
    const file = event.target.files[0];
    if (file) {
      this.uploadImage(file, fileType);
    }
  }

  uploadImage(file: File, fileType: string): void {
    this.imageService.uploadImage(file, fileType, 'Dashboard upload')
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          console.log('Image uploaded successfully!', response.data);
        } else {
          console.error('Failed to upload image:', response.errors);
        }
      });
  }

  uploadEmployeePhoto(file: File): void {
    if (!file) return;
    
    this.imageService.setMyPhoto(file)
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          console.log('Employee photo uploaded successfully!');
        } else {
          console.error('Failed to upload employee photo:', response.errors);
        }
      });
  }

  // Payroll Management - Fixed method calls
  createLeaveRequest(): void {
    if (this.leaveRequestForm.valid) {
      const leaveData = {
        ...this.leaveRequestForm.value,
        startDate: new Date(this.leaveRequestForm.value.startDate),
        endDate: new Date(this.leaveRequestForm.value.endDate)
      };
      
      this.payrollService.createLeaveRequest(leaveData)
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.leaveRequestForm.reset())
        )
        .subscribe(response => {
          if (response.success) {
            console.log('Leave request created successfully!');
            this.loadDashboardStats(); // Refresh stats
          } else {
            console.error('Failed to create leave request:', response.errors);
          }
        });
    }
  }

  // Dashboard Statistics - Fixed method calls
  private loadDashboardStats(): void {
    // Fixed: Use correct parameter signature for getMyLeaveRequests
    this.payrollService.getMyLeaveRequests(1, 1, 'PENDING')
      .pipe(takeUntil(this.destroy$))
      .subscribe(response => {
        if (response.success) {
          this.dashboardStats.pendingLeaveRequests = response.totalCount;
        }
      });
  }

  // Utility Methods
  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  hasRole(role: string): boolean {
    return this.authService.hasRole(role);
  }

  logout(): void {
    this.authService.logout()
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        console.log('Logged out successfully');
      });
  }

  // Helper method to get employee photo URL
  getEmployeePhotoUrl(employeeCode: string): string {
    return this.imageService.getEmployeeAvatarUrl(employeeCode);
  }

  // Helper method to format file size
  formatFileSize(bytes: number): string {
    return this.imageService.formatFileSize(bytes);
  }

  // Helper method to format time
  formatTime(time: string): string {
    return this.workShiftService.formatTime(time);
  }

  // Helper method to calculate worked hours
  calculateWorkedHours(checkIn: Date, checkOut: Date): number {
    return this.attendanceService.calculateWorkedHours(checkIn, checkOut);
  }
}
