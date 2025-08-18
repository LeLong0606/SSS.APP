import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { NotificationService, ToastNotification } from '../../core/services/notification.service';
import { trigger, transition, style, animate, query, stagger } from '@angular/animations';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="toast-container" [@slideIn]>
      <div 
        *ngFor="let toast of toasts; trackBy: trackByToast"
        class="toast toast-{{ toast.type }}"
        [@toastAnimation]
        [class.toast-persistent]="toast.persistent">
        
        <!-- Progress Bar (for loading) -->
        <div 
          *ngIf="toast.progress !== undefined"
          class="toast-progress">
          <div 
            class="toast-progress-bar"
            [style.width.%]="toast.progress">
          </div>
        </div>
        
        <!-- Toast Content -->
        <div class="toast-content">
          <!-- Icon -->
          <div class="toast-icon">
            <div class="icon-wrapper">
              <!-- Success Icon -->
              <svg *ngIf="toast.type === 'success'" viewBox="0 0 24 24" fill="none">
                <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
              </svg>
              
              <!-- Error Icon -->
              <svg *ngIf="toast.type === 'error'" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                <line x1="15" y1="9" x2="9" y2="15" stroke="currentColor" stroke-width="2"/>
                <line x1="9" y1="9" x2="15" y2="15" stroke="currentColor" stroke-width="2"/>
              </svg>
              
              <!-- Warning Icon -->
              <svg *ngIf="toast.type === 'warning'" viewBox="0 0 24 24" fill="none">
                <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z" stroke="currentColor" stroke-width="2"/>
                <line x1="12" y1="9" x2="12" y2="13" stroke="currentColor" stroke-width="2"/>
                <line x1="12" y1="17" x2="12.01" y2="17" stroke="currentColor" stroke-width="2"/>
              </svg>
              
              <!-- Info Icon -->
              <svg *ngIf="toast.type === 'info'" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2"/>
                <line x1="12" y1="16" x2="12" y2="12" stroke="currentColor" stroke-width="2"/>
                <line x1="12" y1="8" x2="12.01" y2="8" stroke="currentColor" stroke-width="2"/>
              </svg>
              
              <!-- Loading Icon -->
              <div *ngIf="toast.type === 'loading'" class="loading-spinner">
                <div class="spinner"></div>
              </div>
            </div>
          </div>
          
          <!-- Text Content -->
          <div class="toast-body">
            <div class="toast-title">{{ toast.title }}</div>
            <div *ngIf="toast.message" class="toast-message">{{ toast.message }}</div>
            
            <!-- Progress Text -->
            <div *ngIf="toast.progress !== undefined" class="toast-progress-text">
              {{ toast.progress }}% complete
            </div>
          </div>
          
          <!-- Actions -->
          <div *ngIf="toast.actions && toast.actions.length > 0" class="toast-actions">
            <button 
              *ngFor="let action of toast.actions"
              class="toast-action btn-{{ action.style || 'secondary' }}"
              (click)="handleAction(action, toast.id)">
              {{ action.label }}
            </button>
          </div>
          
          <!-- Close Button -->
          <button 
            *ngIf="!toast.persistent"
            class="toast-close"
            (click)="closeToast(toast.id)"
            aria-label="Close notification">
            <svg viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2"/>
            </svg>
          </button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./toast-container.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('slideIn', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateX(100%)' }),
          stagger(100, [
            animate('300ms cubic-bezier(0.35, 0, 0.25, 1)', 
              style({ opacity: 1, transform: 'translateX(0)' })
            )
          ])
        ], { optional: true })
      ])
    ]),
    trigger('toastAnimation', [
      transition(':enter', [
        style({ 
          opacity: 0, 
          transform: 'translateX(100%) scale(0.8)',
          height: '0',
          marginBottom: '0'
        }),
        animate('300ms cubic-bezier(0.35, 0, 0.25, 1)', 
          style({ 
            opacity: 1, 
            transform: 'translateX(0) scale(1)',
            height: '*',
            marginBottom: '*'
          })
        )
      ]),
      transition(':leave', [
        style({ overflow: 'hidden' }),
        animate('250ms cubic-bezier(0.35, 0, 0.25, 1)', 
          style({ 
            opacity: 0,
            transform: 'translateX(100%) scale(0.8)',
            height: '0',
            marginBottom: '0',
            paddingTop: '0',
            paddingBottom: '0'
          })
        )
      ])
    ])
  ],
  standalone: false
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  toasts: ToastNotification[] = [];

  constructor(
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.notificationService.toasts$
      .pipe(takeUntil(this.destroy$))
      .subscribe(toasts => {
        this.toasts = toasts;
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  closeToast(id: string): void {
    this.notificationService.hideNotification(id);
  }

  handleAction(action: any, toastId: string): void {
    action.action();
    this.closeToast(toastId);
  }

  trackByToast(index: number, toast: ToastNotification): string {
    return toast.id;
  }
}
