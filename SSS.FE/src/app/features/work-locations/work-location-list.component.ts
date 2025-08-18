import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { Router } from '@angular/router';

import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { WorkLocation } from '../../core/models/work-location.model';
import { UserRole } from '../../core/models/auth.model';

@Component({
  selector: 'app-work-location-list',
  template: `
    <div class="work-location-list-container">
      <div class="header">
        <h2>Địa điểm làm việc</h2>
        <button class="btn btn-primary" *ngIf="canCreate" (click)="createWorkLocation()">
          <span class="icon">+</span>
          Thêm địa điểm
        </button>
      </div>

      <div class="content" *ngIf="!isLoading">
        <div class="table-container">
          <table class="table">
            <thead>
              <tr>
                <th>Tên địa điểm</th>
                <th>Địa chỉ</th>
                <th>Mô tả</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let location of workLocations; trackBy: trackByLocationId">
                <td>{{ location.name }}</td>
                <td>{{ location.address }}</td>
                <td>{{ location.description || 'Không có' }}</td>
                <td>
                  <span class="badge" [class.badge-success]="location.isActive" [class.badge-danger]="!location.isActive">
                    {{ location.isActive ? 'Hoạt động' : 'Không hoạt động' }}
                  </span>
                </td>
                <td>
                  <div class="action-buttons">
                    <button class="btn btn-sm btn-secondary" (click)="viewLocation(location)">
                      Xem
                    </button>
                    <button class="btn btn-sm btn-primary" *ngIf="canEdit" (click)="editLocation(location)">
                      Sửa
                    </button>
                    <button class="btn btn-sm btn-danger" *ngIf="canDelete" (click)="deleteLocation(location)">
                      Xóa
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="loading" *ngIf="isLoading">
        Đang tải dữ liệu...
      </div>
    </div>
  `,
  styles: [`
    .work-location-list-container {
      padding: 20px;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .table-container {
      background: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .action-buttons {
      display: flex;
      gap: 8px;
    }
  `],
  standalone: false
})
export class WorkLocationListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  workLocations: WorkLocation[] = [];
  isLoading = false;
  canCreate = false;
  canEdit = false;
  canDelete = false;

  constructor(
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializePermissions();
    this.loadWorkLocations();
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

  private loadWorkLocations(): void {
    this.isLoading = true;
    
    this.workLocationService.getWorkLocations()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.workLocations = response.data;
          } else {
            this.notificationService.showError('Lỗi khi tải danh sách địa điểm');
          }
        },
        error: (error) => {
          console.error('Error loading work locations:', error);
          this.notificationService.showError('Lỗi khi tải danh sách địa điểm');
        },
        complete: () => {
          this.isLoading = false;
        }
      });
  }

  createWorkLocation(): void {
    this.router.navigate(['/work-locations/create']);
  }

  viewLocation(location: WorkLocation): void {
    this.router.navigate(['/work-locations', location.id]);
  }

  editLocation(location: WorkLocation): void {
    this.router.navigate(['/work-locations', location.id, 'edit']);
  }

  deleteLocation(location: WorkLocation): void {
    if (confirm(`Bạn có chắc chắn muốn xóa địa điểm "${location.name}"?`)) {
      this.workLocationService.deleteWorkLocation(location.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success) {
              this.notificationService.showSuccess('Xóa địa điểm thành công');
              this.loadWorkLocations();
            } else {
              this.notificationService.showError(response.message || 'Lỗi khi xóa địa điểm');
            }
          },
          error: (error) => {
            console.error('Error deleting work location:', error);
            this.notificationService.showError('Lỗi khi xóa địa điểm');
          }
        });
    }
  }

  trackByLocationId(index: number, location: WorkLocation): number {
    return location.id;
  }
}
