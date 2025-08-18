import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { WorkLocationService } from '../../core/services/work-location.service';
import { NotificationService } from '../../core/services/notification.service';
import { WorkLocation, CreateWorkLocationRequest, UpdateWorkLocationRequest } from '../../core/models/work-location.model';
import { ApiResponse } from '../../core/models/api-response.model';

@Component({
  selector: 'app-work-location-form',
  template: `
    <div class="work-location-form-container">
      <div class="header">
        <h2>{{ isEditMode ? 'Sửa địa điểm' : 'Thêm địa điểm mới' }}</h2>
      </div>

      <form [formGroup]="locationForm" (ngSubmit)="onSubmit()" class="location-form">
        <div class="form-group">
          <label for="name" class="form-label">Tên địa điểm *</label>
          <input type="text" id="name" class="form-control" formControlName="name" 
                 [class.is-invalid]="isFieldInvalid('name')" 
                 placeholder="Nhập tên địa điểm">
          <div class="invalid-feedback" *ngIf="isFieldInvalid('name')">
            {{ getFieldError('name') }}
          </div>
        </div>

        <div class="form-group">
          <label for="address" class="form-label">Địa chỉ *</label>
          <input type="text" id="address" class="form-control" formControlName="address" 
                 [class.is-invalid]="isFieldInvalid('address')" 
                 placeholder="Nhập địa chỉ">
          <div class="invalid-feedback" *ngIf="isFieldInvalid('address')">
            {{ getFieldError('address') }}
          </div>
        </div>

        <div class="form-group">
          <label for="description" class="form-label">Mô tả</label>
          <textarea id="description" class="form-control" formControlName="description" 
                    rows="3" placeholder="Nhập mô tả (tùy chọn)"></textarea>
        </div>

        <div class="form-group">
          <label class="form-check">
            <input type="checkbox" class="form-check-input" formControlName="isActive">
            <span class="form-check-label">Địa điểm hoạt động</span>
          </label>
        </div>

        <div class="form-actions">
          <button type="button" class="btn btn-secondary" (click)="goBack()" [disabled]="isLoading">
            Hủy
          </button>
          <button type="submit" class="btn btn-primary" [disabled]="locationForm.invalid || isLoading">
            {{ isEditMode ? 'Cập nhật' : 'Tạo mới' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .work-location-form-container {
      max-width: 600px;
      margin: 0 auto;
      padding: 20px;
    }
    .location-form {
      background: white;
      padding: 30px;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    .form-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      margin-top: 24px;
    }
  `],
  standalone: false
})
export class WorkLocationFormComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  locationForm!: FormGroup;
  isEditMode = false;
  locationId: string = ''; // ✅ FIX: Change to string to match service
  isLoading = false;

  constructor(
    private formBuilder: FormBuilder,
    private workLocationService: WorkLocationService,
    private notificationService: NotificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.createForm();
    this.checkEditMode();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): void {
    this.locationForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.required, Validators.maxLength(255)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.locationId = id; // ✅ FIX: Keep as string
      this.loadWorkLocation();
    }
  }

  private loadWorkLocation(): void {
    if (!this.locationId) return;

    this.isLoading = true;
    
    // ✅ FIX: Use correct method name from service
    this.workLocationService.getWorkLocation(this.locationId) // Changed from getWorkLocationById
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<WorkLocation>): void => { // ✅ FIX: Explicit types
          if (response.success && response.data) {
            this.locationForm.patchValue(response.data);
          } else {
            this.notificationService.showError('Không tìm thấy địa điểm');
            this.goBack();
          }
        },
        error: (error: any): void => { // ✅ FIX: Explicit type
          console.error('Error loading work location:', error);
          this.notificationService.showError('Lỗi khi tải thông tin địa điểm');
          this.goBack();
        },
        complete: (): void => { // ✅ FIX: Explicit return type
          this.isLoading = false;
        }
      });
  }

  onSubmit(): void {
    if (this.locationForm.valid && !this.isLoading) {
      this.isLoading = true;
      
      if (this.isEditMode) {
        this.updateWorkLocation();
      } else {
        this.createWorkLocation();
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  private createWorkLocation(): void {
    const request: CreateWorkLocationRequest = this.locationForm.value;

    this.workLocationService.createWorkLocation(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<WorkLocation>): void => { // ✅ FIX: Explicit types
          if (response.success) {
            this.notificationService.showSuccess('Tạo địa điểm thành công');
            this.router.navigate(['/work-locations']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi tạo địa điểm');
          }
        },
        error: (error: any): void => { // ✅ FIX: Explicit type
          console.error('Error creating work location:', error);
          this.notificationService.showError('Lỗi khi tạo địa điểm');
        },
        complete: (): void => { // ✅ FIX: Explicit return type
          this.isLoading = false;
        }
      });
  }

  private updateWorkLocation(): void {
    if (!this.locationId) return;

    const request: UpdateWorkLocationRequest = {
      id: this.locationId, // ✅ FIX: Already string
      ...this.locationForm.value
    };

    // ✅ FIX: Pass string ID as expected by service
    this.workLocationService.updateWorkLocation(this.locationId, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<WorkLocation>): void => { // ✅ FIX: Explicit types
          if (response.success) {
            this.notificationService.showSuccess('Cập nhật địa điểm thành công');
            this.router.navigate(['/work-locations']);
          } else {
            this.notificationService.showError(response.message || 'Lỗi khi cập nhật địa điểm');
          }
        },
        error: (error: any): void => { // ✅ FIX: Explicit type
          console.error('Error updating work location:', error);
          this.notificationService.showError('Lỗi khi cập nhật địa điểm');
        },
        complete: (): void => { // ✅ FIX: Explicit return type
          this.isLoading = false;
        }
      });
  }

  // Form validation helpers
  isFieldInvalid(fieldName: string): boolean {
    const field = this.locationForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.locationForm.get(fieldName);
    
    if (field?.errors) {
      if (field.errors['required']) {
        return this.getRequiredErrorMessage(fieldName);
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldLabel(fieldName)} quá dài`;
      }
    }
    
    return '';
  }

  private getRequiredErrorMessage(fieldName: string): string {
    return `${this.getFieldLabel(fieldName)} là bắt buộc`;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      name: 'Tên địa điểm',
      address: 'Địa chỉ',
      description: 'Mô tả'
    };
    
    return labels[fieldName] || fieldName;
  }

  private markFormGroupTouched(): void {
    Object.keys(this.locationForm.controls).forEach(key => {
      const control = this.locationForm.get(key);
      control?.markAsTouched();
    });
  }

  goBack(): void {
    this.router.navigate(['/work-locations']);
  }
}
