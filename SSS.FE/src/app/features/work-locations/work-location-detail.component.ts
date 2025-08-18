import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';
import { WorkLocation } from '../../core/models/work-location.model';
import { ApiResponse } from '../../core/models/api-response.model';

@Component({
  selector: 'app-work-location-detail',
  template: `
    <div class="work-location-detail-container">
      <div class="header">
        <h2>Chi tiết địa điểm làm việc</h2>
        <div class="actions">
          <button class="btn btn-secondary" (click)="goBack()">
            ← Quay lại
          </button>
          <button class="btn btn-primary" (click)="editLocation()" *ngIf="workLocation">
            Chỉnh sửa
          </button>
        </div>
      </div>

      <div class="content" *ngIf="workLocation && !isLoading">
        <div class="detail-card">
          <div class="card-body">
            <div class="detail-row">
              <label>Tên địa điểm:</label>
              <span>{{ workLocation.name }}</span>
            </div>
            
            <div class="detail-row">
              <label>Địa chỉ:</label>
              <span>{{ workLocation.address }}</span>
            </div>
            
            <div class="detail-row">
              <label>Mô tả:</label>
              <span>{{ workLocation.description || 'Không có mô tả' }}</span>
            </div>
            
            <div class="detail-row">
              <label>Trạng thái:</label>
              <span class="badge" [class.badge-success]="workLocation.isActive" [class.badge-danger]="!workLocation.isActive">
                {{ workLocation.isActive ? 'Hoạt động' : 'Không hoạt động' }}
              </span>
            </div>
            
            <div class="detail-row" *ngIf="workLocation.createdAt">
              <label>Ngày tạo:</label>
              <span>{{ formatDate(workLocation.createdAt) }}</span>
            </div>
            
            <div class="detail-row" *ngIf="workLocation.updatedAt">
              <label>Ngày cập nhật:</label>
              <span>{{ formatDate(workLocation.updatedAt) }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="loading" *ngIf="isLoading">
        <p>Đang tải thông tin địa điểm...</p>
      </div>

      <div class="error" *ngIf="!workLocation && !isLoading">
        <p>Không tìm thấy thông tin địa điểm</p>
        <button class="btn btn-secondary" (click)="goBack()">Quay lại danh sách</button>
      </div>
    </div>
  `,
  styles: [`
    .work-location-detail-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 20px;
    }
    
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }
    
    .actions {
      display: flex;
      gap: 12px;
    }
    
    .detail-card {
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      overflow: hidden;
    }
    
    .card-body {
      padding: 24px;
    }
    
    .detail-row {
      display: flex;
      margin-bottom: 16px;
      align-items: flex-start;
    }
    
    .detail-row label {
      font-weight: 600;
      min-width: 140px;
      color: #374151;
    }
    
    .detail-row span {
      color: #6b7280;
      flex: 1;
    }
    
    .loading, .error {
      text-align: center;
      padding: 40px;
      color: #6b7280;
    }
  `],
  standalone: false
})
export class WorkLocationDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  workLocation: WorkLocation | null = null;
  isLoading = false;
  locationId: string = ''; // ✅ FIX: Change to string to match service

  constructor(
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadWorkLocation();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadWorkLocation(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/work-locations']);
      return;
    }

    this.locationId = id; // ✅ FIX: Keep as string
    this.isLoading = true;

    // ✅ FIX: Use correct method name from service
    this.workLocationService.getWorkLocation(this.locationId) // Changed from getWorkLocationById
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<WorkLocation>): void => { // ✅ FIX: Explicit types
          if (response.success && response.data) {
            this.workLocation = response.data;
          } else {
            this.notificationService.showError('Không tìm thấy địa điểm');
          }
        },
        error: (error: any): void => { // ✅ FIX: Explicit type
          console.error('Error loading work location:', error);
          this.notificationService.showError('Lỗi khi tải thông tin địa điểm');
        },
        complete: (): void => { // ✅ FIX: Explicit return type
          this.isLoading = false;
        }
      });
  }

  editLocation(): void {
    if (this.workLocation) {
      this.router.navigate(['/work-locations', this.workLocation.id, 'edit']);
    }
  }

  goBack(): void {
    this.router.navigate(['/work-locations']);
  }

  formatDate(date: Date | string): string {
    if (!date) return 'Không có thông tin';
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}
