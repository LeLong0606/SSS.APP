import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil, finalize } from 'rxjs';

import { EmployeeService } from '../../core/services/employee.service';
import { DepartmentService } from '../../core/services/department.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { Employee, EmployeeFilter } from '../../core/models/employee.model';
import { Department } from '../../core/models/department.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.scss'],
  standalone: false
})
export class EmployeeListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  // Data properties
  employees: Employee[] = [];
  departments: Department[] = [];
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalRecords = 0;
  totalPages = 0;

  // Search and filters
  searchTerm = '';
  selectedDepartment = '';
  selectedStatus = '';
  showTeamLeadersOnly = false;
  
  // UI state
  isLoading = false;
  isSearching = false;
  showCreateForm = false;
  selectedEmployee: Employee | null = null;

  // Permissions
  canCreate = false;
  canEdit = false;
  canDelete = false;
  canManageStatus = false;

  // Filter options
  statusOptions = [
    { value: '', label: 'Tất cả trạng thái' },
    { value: 'active', label: 'Đang làm việc' },
    { value: 'inactive', label: 'Đã nghỉ việc' }
  ];

  pageSizeOptions = [5, 10, 20, 50];

  constructor(
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.initializeSearch();
    this.loadDepartments();
    this.loadEmployees();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR, UserRole.TEAM_LEADER]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canManageStatus = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
  }

  private initializeSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.currentPage = 1;
      this.loadEmployees();
    });
  }

  private loadDepartments(): void {
    this.departmentService.getAllDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.departments = response.data;
          }
        },
        error: (error) => {
          console.error('Error loading departments:', error);
          this.notificationService.showError('Lỗi khi tải danh sách phòng ban');
        }
      });
  }

  loadEmployees(): void {
    this.isLoading = true;

    const filter: EmployeeFilter = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm || undefined,
      departmentId: this.selectedDepartment ? parseInt(this.selectedDepartment) : undefined,
      isTeamLeader: this.showTeamLeadersOnly || undefined,
      includeInactive: this.selectedStatus === 'inactive' || this.selectedStatus === ''
    };

    this.employeeService.getEmployees(filter)
      .pipe(
        finalize(() => this.isLoading = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.employees = response.data;
            this.totalRecords = response.totalCount;
            this.totalPages = response.totalPages;
          } else {
            this.employees = [];
            this.totalRecords = 0;
            this.totalPages = 0;
            this.notificationService.showError(response.message || 'Lỗi khi tải danh sách nhân viên');
          }
        },
        error: (error) => {
          console.error('Error loading employees:', error);
          this.notificationService.showError('Lỗi khi tải danh sách nhân viên');
          this.employees = [];
          this.totalRecords = 0;
          this.totalPages = 0;
        }
      });
  }

  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadEmployees();
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadEmployees();
    }
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadEmployees();
  }

  // Employee actions
  createEmployee(): void {
    if (!this.canCreate) {
      this.notificationService.showError('Bạn không có quyền tạo nhân viên mới');
      return;
    }
    
    this.router.navigate(['/employees/create']);
  }

  viewEmployee(employee: Employee): void {
    this.router.navigate(['/employees', employee.id]);
  }

  editEmployee(employee: Employee): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa nhân viên');
      return;
    }

    this.router.navigate(['/employees', employee.id, 'edit']);
  }

  deleteEmployee(employee: Employee): void {
    if (!this.canDelete) {
      this.notificationService.showError('Bạn không có quyền xóa nhân viên');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa nhân viên ${employee.fullName}?`)) {
      this.isLoading = true;
      
      this.employeeService.deleteEmployee(employee.id)
        .pipe(
          finalize(() => this.isLoading = false),
          takeUntil(this.destroy$)
        )
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa nhân viên thành công');
              this.loadEmployees();
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

  toggleEmployeeStatus(employee: Employee): void {
    if (!this.canManageStatus) {
      this.notificationService.showError('Bạn không có quyền thay đổi trạng thái nhân viên');
      return;
    }

    const newStatus = !employee.isActive;
    const action = newStatus ? 'kích hoạt' : 'vô hiệu hóa';

    if (confirm(`Bạn có chắc chắn muốn ${action} nhân viên ${employee.fullName}?`)) {
      this.employeeService.toggleEmployeeStatus(employee.id, newStatus)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess(`${action} nhân viên thành công`);
              employee.isActive = newStatus;
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

  // Utility methods
  getDepartmentName(departmentId?: number): string {
    if (!departmentId) return 'Không có';
    
    const department = this.departments.find(d => d.id === departmentId);
    return department ? department.name : 'Không xác định';
  }

  getStatusText(isActive: boolean): string {
    return isActive ? 'Đang làm việc' : 'Đã nghỉ việc';
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-inactive';
  }

  getTeamLeaderBadgeClass(isTeamLeader: boolean): string {
    return isTeamLeader ? 'badge-team-leader' : '';
  }

  formatDate(date: Date | string): string {
    if (!date) return '';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN');
  }

  formatCurrency(amount: number | undefined | null): string {
    if (!amount) return '';
    return new Intl.NumberFormat('vi-VN', { 
      style: 'currency', 
      currency: 'VND' 
    }).format(amount);
  }

  // Pagination helpers
  getPaginationNumbers(): number[] {
    const delta = 2;
    const range = [];
    const rangeWithDots = [];

    for (let i = Math.max(2, this.currentPage - delta); 
         i <= Math.min(this.totalPages - 1, this.currentPage + delta); 
         i++) {
      range.push(i);
    }

    if (this.currentPage - delta > 2) {
      rangeWithDots.push(1, -1);
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (this.currentPage + delta < this.totalPages - 1) {
      rangeWithDots.push(-1, this.totalPages);
    } else if (this.totalPages > 1) {
      rangeWithDots.push(this.totalPages);
    }

    return rangeWithDots;
  }

  // Permission check methods for template
  canEditEmployee(): boolean {
    return this.canEdit;
  }

  canDeleteEmployee(): boolean {
    return this.canDelete;
  }

  canCreateEmployee(): boolean {
    return this.canCreate;
  }

  // Clear filters
  clearFilters(): void {
    this.searchTerm = '';
    this.selectedDepartment = '';
    this.selectedStatus = '';
    this.showTeamLeadersOnly = false;
    this.currentPage = 1;
    this.loadEmployees();
  }

  // Refresh data
  refresh(): void {
    this.loadEmployees();
    this.loadDepartments();
  }

  // Statistics methods
  getActiveEmployeesCount(): number {
    return this.employees.filter(emp => emp.isActive).length;
  }

  getTeamLeadersCount(): number {
    return this.employees.filter(emp => emp.isTeamLeader).length;
  }

  // Track by function for better performance
  trackByEmployeeId(index: number, employee: Employee): number {
    return employee.id;
  }
}
