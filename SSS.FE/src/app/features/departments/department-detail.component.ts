import { Component, OnInit, OnDestroy } from '@angular/core';
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
  selector: 'app-department-detail',
  templateUrl: './department-detail.component.html',
  styleUrls: ['./department-detail.component.scss'],
  standalone: false
})
export class DepartmentDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  departmentId: number = 0;
  department: Department | null = null;
  employees: Employee[] = [];
  
  isLoading = false;
  isLoadingEmployees = false;
  
  // Permissions
  canEdit = false;
  canDelete = false;
  canAssignTeamLeader = false;

  // Tabs
  activeTab = 'overview';

  constructor(
    private departmentService: DepartmentService,
    private employeeService: EmployeeService,
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
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR]);
    this.canAssignTeamLeader = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private initializeRoute(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params['id'];
      if (id) {
        this.departmentId = +id;
        this.loadDepartment();
        this.loadDepartmentEmployees();
      } else {
        this.notificationService.showError('ID phòng ban không hợp lệ');
        this.router.navigate(['/departments']);
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

  private loadDepartmentEmployees(): void {
    this.isLoadingEmployees = true;
    
    this.employeeService.getEmployeesByDepartment(this.departmentId, { pageNumber: 1, pageSize: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employees = response.data;
          } else {
            this.employees = [];
          }
        },
        error: (error) => {
          console.error('Error loading department employees:', error);
          this.employees = [];
        },
        complete: () => {
          this.isLoadingEmployees = false;
        }
      });
  }

  // Navigation methods
  onEdit(): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa phòng ban');
      return;
    }
    this.router.navigate(['/departments', this.departmentId, 'edit']);
  }

  onDelete(): void {
    if (!this.canDelete) {
      this.notificationService.showError('Bạn không có quyền xóa phòng ban');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa phòng ban ${this.department?.name}?`)) {
      this.isLoading = true;
      
      this.departmentService.deleteDepartment(this.departmentId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa phòng ban thành công');
              this.router.navigate(['/departments']);
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa phòng ban');
            }
          },
          error: (error) => {
            console.error('Error deleting department:', error);
            this.notificationService.showError('Lỗi khi xóa phòng ban');
          },
          complete: () => {
            this.isLoading = false;
          }
        });
    }
  }

  onBackToList(): void {
    this.router.navigate(['/departments']);
  }

  // Team leader management
  assignTeamLeader(employeeCode: string): void {
    if (!this.canAssignTeamLeader) {
      this.notificationService.showError('Bạn không có quyền phân công trưởng phòng');
      return;
    }

    this.departmentService.assignTeamLeader(this.departmentId, employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Phân công trưởng phòng thành công');
            this.loadDepartment();
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi phân công trưởng phòng');
          }
        },
        error: (error) => {
          console.error('Error assigning team leader:', error);
          this.notificationService.showError('Lỗi khi phân công trưởng phòng');
        }
      });
  }

  removeTeamLeader(): void {
    if (!this.canAssignTeamLeader) {
      this.notificationService.showError('Bạn không có quyền bỏ nhiệm trưởng phòng');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn bỏ nhiệm trưởng phòng của ${this.department?.name}?`)) {
      this.departmentService.removeTeamLeader(this.departmentId)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Bỏ nhiệm trưởng phòng thành công');
              this.loadDepartment();
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi bỏ nhiệm trưởng phòng');
            }
          },
          error: (error) => {
            console.error('Error removing team leader:', error);
            this.notificationService.showError('Lỗi khi bỏ nhiệm trưởng phòng');
          }
        });
    }
  }

  // Tab management
  setActiveTab(tab: string): void {
    this.activeTab = tab;
  }

  // Utility methods
  getStatusText(isActive: boolean): string {
    return isActive ? 'Đang hoạt động' : 'Ngừng hoạt động';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-inactive';
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

  // Employee management
  viewEmployee(employee: Employee): void {
    this.router.navigate(['/employees', employee.id]);
  }

  // Refresh data
  refresh(): void {
    this.loadDepartment();
    this.loadDepartmentEmployees();
  }

  // Track by functions
  trackByEmployeeId(index: number, employee: Employee): number {
    return employee.id;
  }
}
