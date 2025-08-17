import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil, finalize } from 'rxjs';

import { DepartmentService } from '../../core/services/department.service';
import { EmployeeService } from '../../core/services/employee.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

import { Department, DepartmentFilter } from '../../core/models/department.model';
import { Employee } from '../../core/models/employee.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-department-list',
  templateUrl: './department-list.component.html',
  styleUrls: ['./department-list.component.scss'],
  standalone: false
})
export class DepartmentListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  // Data properties
  departments: Department[] = [];
  employees: Employee[] = [];
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalRecords = 0;
  totalPages = 0;

  // Search and filters
  searchTerm = '';
  selectedStatus = '';
  
  // UI state
  isLoading = false;
  selectedDepartment: Department | null = null;

  // Permissions
  canCreate = false;
  canEdit = false;
  canDelete = false;

  // Filter options
  statusOptions = [
    { value: '', label: 'Tất cả trạng thái' },
    { value: 'active', label: 'Đang hoạt động' },
    { value: 'inactive', label: 'Ngừng hoạt động' }
  ];

  pageSizeOptions = [5, 10, 20, 50];

  constructor(
    private departmentService: DepartmentService,
    private employeeService: EmployeeService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.initializeSearch();
    this.loadEmployees();
    this.loadDepartments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializePermissions(): void {
    this.canCreate = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canEdit = this.authService.hasAnyRole([UserRole.ADMINISTRATOR, UserRole.DIRECTOR]);
    this.canDelete = this.authService.hasAnyRole([UserRole.ADMINISTRATOR]);
  }

  private initializeSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.currentPage = 1;
      this.loadDepartments();
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

  loadDepartments(): void {
    this.isLoading = true;

    const filter: DepartmentFilter = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm || undefined,
      includeInactive: this.selectedStatus === 'inactive' || this.selectedStatus === ''
    };

    this.departmentService.getDepartments(filter)
      .pipe(
        finalize(() => this.isLoading = false),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.departments = response.data;
            this.totalRecords = response.totalCount;
            this.totalPages = response.totalPages;
          } else {
            this.departments = [];
            this.totalRecords = 0;
            this.totalPages = 0;
            this.notificationService.showError(response.message || 'Lỗi khi tải danh sách phòng ban');
          }
        },
        error: (error) => {
          console.error('Error loading departments:', error);
          this.notificationService.showError('Lỗi khi tải danh sách phòng ban');
          this.departments = [];
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
    this.loadDepartments();
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadDepartments();
    }
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.loadDepartments();
  }

  // Department actions
  createDepartment(): void {
    if (!this.canCreate) {
      this.notificationService.showError('Bạn không có quyền tạo phòng ban mới');
      return;
    }
    
    this.router.navigate(['/departments/create']);
  }

  viewDepartment(department: Department): void {
    this.router.navigate(['/departments', department.id]);
  }

  editDepartment(department: Department): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền chỉnh sửa phòng ban');
      return;
    }

    this.router.navigate(['/departments', department.id, 'edit']);
  }

  deleteDepartment(department: Department): void {
    if (!this.canDelete) {
      this.notificationService.showError('Bạn không có quyền xóa phòng ban');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn xóa phòng ban ${department.name}?`)) {
      this.isLoading = true;
      
      this.departmentService.deleteDepartment(department.id)
        .pipe(
          finalize(() => this.isLoading = false),
          takeUntil(this.destroy$)
        )
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa phòng ban thành công');
              this.loadDepartments();
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa phòng ban');
            }
          },
          error: (error) => {
            console.error('Error deleting department:', error);
            this.notificationService.showError('Lỗi khi xóa phòng ban');
          }
        });
    }
  }

  // Assign/Remove team leader
  assignTeamLeader(department: Department, employeeCode: string): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền phân công trưởng phòng');
      return;
    }

    this.departmentService.assignTeamLeader(department.id, employeeCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.notificationService.showSuccess('Phân công trưởng phòng thành công');
            this.loadDepartments();
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

  removeTeamLeader(department: Department): void {
    if (!this.canEdit) {
      this.notificationService.showError('Bạn không có quyền bỏ nhiệm trưởng phòng');
      return;
    }

    if (confirm(`Bạn có chắc chắn muốn bỏ nhiệm trưởng phòng của ${department.name}?`)) {
      this.departmentService.removeTeamLeader(department.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Bỏ nhiệm trưởng phòng thành công');
              this.loadDepartments();
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

  // Permission check methods for template
  canCreateDepartment(): boolean {
    return this.canCreate;
  }

  canEditDepartment(): boolean {
    return this.canEdit;
  }

  canDeleteDepartment(): boolean {
    return this.canDelete;
  }

  // Clear filters
  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.currentPage = 1;
    this.loadDepartments();
  }

  // Refresh data
  refresh(): void {
    this.loadDepartments();
    this.loadEmployees();
  }

  // Track by function for better performance
  trackByDepartmentId(index: number, department: Department): number {
    return department.id;
  }

  // Get available team leaders (employees who are not already team leaders elsewhere)
  getAvailableTeamLeaders(): Employee[] {
    const assignedLeaderCodes = this.departments
      .filter(dept => dept.teamLeaderEmployeeCode)
      .map(dept => dept.teamLeaderEmployeeCode);
    
    return this.employees.filter(emp => 
      emp.isActive && 
      (emp.isTeamLeader || !assignedLeaderCodes.includes(emp.employeeCode))
    );
  }
}
