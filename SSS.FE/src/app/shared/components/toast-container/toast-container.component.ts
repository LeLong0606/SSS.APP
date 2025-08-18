import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
// ✅ FIX: Use direct import path
import { NotificationService, ToastNotification, NotificationAction } from '../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toast-container">
      <div *ngFor="let toast of toasts; trackBy: trackByToastId" 
           class="toast" 
           [class]="'toast-' + toast.type">
        
        <div class="toast-header">
          <div class="toast-icon">
            {{ getToastIcon(toast.type) }}
          </div>
          <div class="toast-content">
            <div class="toast-title">{{ toast.title }}</div>
            <div class="toast-message" *ngIf="toast.message">{{ toast.message }}</div>
          </div>
          <button class="toast-close" (click)="closeToast(toast.id)">
            ×
          </button>
        </div>

        <!-- Progress bar for loading toasts -->
        <div class="toast-progress" *ngIf="toast.type === 'loading' && toast.progress !== undefined">
          <div class="progress-bar" [style.width.%]="toast.progress"></div>
        </div>

        <!-- Action buttons -->
        <div class="toast-actions" *ngIf="toast.actions && toast.actions.length > 0">
          <button *ngFor="let action of toast.actions" 
                  class="toast-action-btn" 
                  [class]="'btn-' + (action.style || 'primary')"
                  (click)="executeAction(action, toast.id)">
            {{ action.label }}
          </button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./toast-container.component.scss'],
  standalone: false
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  toasts: ToastNotification[] = [];

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    // Subscribe to toast notifications
    this.notificationService.toasts$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (toasts: ToastNotification[]): void => {
          this.toasts = toasts;
        },
        error: (error: any): void => {
          console.error('Error loading toasts:', error);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  closeToast(id: string): void {
    this.notificationService.hideNotification(id);
  }

  executeAction(action: NotificationAction, toastId: string): void {
    try {
      action.action();
      this.closeToast(toastId);
    } catch (error) {
      console.error('Error executing toast action:', error);
    }
  }

  getToastIcon(type: string): string {
    const icons: { [key: string]: string } = {
      success: '✅',
      error: '❌',
      warning: '⚠️',
      info: 'ℹ️',
      loading: '⏳'
    };
    
    return icons[type] || 'ℹ️';
  }

  trackByToastId(index: number, toast: ToastNotification): string {
    return toast.id;
  }
}
