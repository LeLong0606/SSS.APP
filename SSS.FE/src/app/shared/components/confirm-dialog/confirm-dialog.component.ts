import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <div class="modal" *ngIf="isVisible" (click)="onBackdropClick($event)">
      <div class="modal-dialog" (click)="$event.stopPropagation()">
        <div class="modal-content">
          <div class="modal-header">
            <h4 class="modal-title">{{ title }}</h4>
            <button type="button" class="btn-close" (click)="onCancel()">
              <span>&times;</span>
            </button>
          </div>
          <div class="modal-body">
            <p>{{ message }}</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" (click)="onCancel()">
              {{ cancelText }}
            </button>
            <button type="button" class="btn" [class]="confirmButtonClass" (click)="onConfirm()">
              {{ confirmText }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./confirm-dialog.component.scss'],
  standalone: false
})
export class ConfirmDialogComponent {
  @Input() isVisible: boolean = false;
  @Input() title: string = 'Xác nhận';
  @Input() message: string = 'Bạn có chắc chắn muốn thực hiện hành động này?';
  @Input() confirmText: string = 'Xác nhận';
  @Input() cancelText: string = 'Hủy bỏ';
  @Input() type: 'danger' | 'warning' | 'info' = 'danger';

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  get confirmButtonClass(): string {
    switch (this.type) {
      case 'danger':
        return 'btn-danger';
      case 'warning':
        return 'btn-warning';
      case 'info':
        return 'btn-primary';
      default:
        return 'btn-primary';
    }
  }

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}
