import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil, finalize } from 'rxjs';

import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { Employee } from '../../core/models/employee.model';
import { Department } from '../../core/models/department.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-employee-detail',
  templateUrl: './employee-detail.component.html',
  styleUrls: ['./employee-detail.component.scss'],
  standalone: false
})
export class EmployeeDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  employee: Employee | null = null;
  department: Department | null = null;
  isLoading = false;
  employeeId: number | null = null;

  // Permissions
  canEdit = false;
  canDelete = false;
  canManageStatus = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private notificationService: NotificationService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.getEmployeeId();
    this.loadEmployee();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canManageStatus = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private getEmployeeId(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.employeeId = parseInt(id, 10);
    } else {
      this.router.navigate(['/employees']);
    }
  }

  private loadEmployee(): void {
    if (!this.employeeId) return;

    this.isLoading = true;

    this.employeeService.getEmployee(this.employeeId)
      .pipe(
        finalize(() => this.isLoading = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employee = response.data;
            if (this.employee.departmentId) {
              this.loadDepartment(this.employee.departmentId);
            }
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

  private loadDepartment(departmentId: number): void {
    this.departmentService.getDepartment(departmentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.department = response.data;
          }
        },
        error: (error) => {
          console.error('Error loading department:', error);
        }
      });
  }

  // Actions
  editEmployee(): void {
    if (!this.canEdit || !this.employee) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa nhân viên');
      return;
    }

    this.router.navigate(['/employees', this.employee.id, 'edit']);
  }

  deleteEmployee(): void {
    if (!this.canDelete || !this.employee) {
      this.notificationService.showError('Bạn không có quyền xóa nhân viên');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa nhân viên ${this.employee.fullName}?`)) {
      this.employeeService.deleteEmployee(this.employee.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa nhân viên thành công');
              this.router.navigate(['/employees']);
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa nhân viên');
            }
          },
          error: (error) => {
            console.error('Error deleting employee:', error);
            this.notificationService.showError('Lỗi khi xóa nhân viên');
          }
        });
    }
  }

  toggleEmployeeStatus(): void {
    if (!this.canManageStatus || !this.employee) {
      this.notificationService.showError('Bạn không có quyền thay đổi trạng thái nhân viên');
      return;
    }

    const newStatus = !this.employee.isActive;
    const action = newStatus ? 'kích hoạt' : 'vô hiệu hóa';

    if (confirm(`Bạn có chắc chắn muốn ${action} nhân viên ${this.employee.fullName}?`)) {
      this.employeeService.toggleEmployeeStatus(this.employee.id, newStatus)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess(`${action} nhân viên thành công`);
              if (this.employee) {
                this.employee.isActive = newStatus;
              }
            } else {
              this.notificationService.showError(response.message || `Lỗi khi ${action} nhân viên`);
            }
          },
          error: (error) => {
            console.error(`Error toggling employee status:`, error);
            this.notificationService.showError(`Lỗi khi ${action} nhân viên`);
          }
        });
    }
  }

  // Navigation
  goBack(): void {
    this.router.navigate(['/employees']);
  }

  // Utility methods
  formatDate(date: Date | string | undefined): string {
    if (!date) return 'Chưa có thông tin';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN');
  }

  formatCurrency(amount: number | undefined | null): string {
    if (!amount) return 'Chưa có thông tin';
    
    return new Intl.NumberFormat('vi-VN', { 
      style: 'currency', 
      currency: 'VND' 
    }).format(amount);
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Đang làm việc' : 'Đã nghỉ việc';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-inactive';
  }

  getStatusActionText(): string {
    if (!this.employee) return '';
    return this.employee.isActive ? 'Vô hiệu hóa' : 'Kích hoạt';
  }

  getStatusActionClass(): string {
    if (!this.employee) return '';
    return this.employee.isActive ? 'btn-warning' : 'btn-success';
  }

  getDepartmentName(): string {
    return this.department?.name || 'Chưa phân bổ';
  }

  getEmployeeAge(): number | null {
    if (!this.employee?.hireDate) return null;
    
    const hireDate = new Date(this.employee.hireDate);
    const today = new Date();
    const diffTime = Math.abs(today.getTime() - hireDate.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    const diffYears = Math.floor(diffDays / 365);
    
    return diffYears;
  }

  getWorkingYears(): number | null {
    return this.getEmployeeAge();
  }

  hasContactInfo(): boolean {
    return !!(this.employee?.phoneNumber || this.employee?.address);
  }

  hasWorkInfo(): boolean {
    return !!(this.employee?.hireDate || this.employee?.salary || this.employee?.departmentId);
  }
}
