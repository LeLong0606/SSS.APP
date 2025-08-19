// ...existing code...
// ...imports and interfaces remain...
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer } from 'rxjs';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info' | 'loading';
  title: string;
  message?: string;
  duration?: number;
  persistent?: boolean;
  actions?: NotificationAction[];
  data?: any;
  timestamp: Date;
  read: boolean;
  priority: 'low' | 'normal' | 'high' | 'urgent';
  category?: string;
  icon?: string;
  image?: string;
  progress?: number;
}

export interface NotificationAction {
  label: string;
  action: () => void;
  style?: 'primary' | 'secondary' | 'danger';
}

export interface ToastNotification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info' | 'loading';
  title: string;
  message?: string;
  duration: number;
  persistent: boolean;
  actions?: NotificationAction[];
  progress?: number;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  private toastsSubject = new BehaviorSubject<ToastNotification[]>([]);
  private unreadCountSubject = new BehaviorSubject<number>(0);
  
  public notifications$ = this.notificationsSubject.asObservable();
  public toasts$ = this.toastsSubject.asObservable();
  public unreadCount$ = this.unreadCountSubject.asObservable();

  constructor() {
    this.loadStoredNotifications();
  }

  /**
   * Speak notification aloud using browser speech synthesis
   */
  private speakNotification(title: string, message?: string): void {
    // Audio announcements disabled by request
    return;
  }

  /**
   * Show success notification
   */
  showSuccess(title: string, message?: string, options?: Partial<Notification>): string {
    this.speakNotification(title, message);
    return this.show({
      type: 'success',
      title,
      message,
      duration: 8000,
      priority: 'high',
      persistent: true,
      ...options
    });
  }

  /**
   * Show error notification
   */
  showError(title: string, message?: string, options?: Partial<Notification>): string {
    this.speakNotification(title, message);
    return this.show({
      type: 'error',
      title,
      message,
      icon: '❌',
      duration: 8000,
      priority: 'high',
      persistent: true,
      ...options
    });
  }

  /**
   * Show warning notification
   */
  showWarning(title: string, message?: string, options?: Partial<Notification>): string {
    this.speakNotification(title, message);
    return this.show({
      type: 'warning',
      title,
      message,
      icon: '⚠️',
      duration: 6000,
      priority: 'normal',
      ...options
    });
  }

  /**
   * Show info notification
   */
  showInfo(title: string, message?: string, options?: Partial<Notification>): string {
    this.speakNotification(title, message);
    return this.show({
      type: 'info',
      title,
      message,
      icon: 'ℹ️',
      duration: 5000,
      priority: 'normal',
      ...options
    });
  }

  /**
   * Show loading notification with progress
   */
  showLoading(title: string, message?: string): string {
    return this.show({
      type: 'loading',
      title,
      message,
      icon: '⏳',
      persistent: true,
      progress: 0,
      priority: 'normal'
    });
  }

  /**
   * Update loading notification progress
   */
  updateProgress(id: string, progress: number, message?: string): void {
    const notifications = this.notificationsSubject.value;
    const notification = notifications.find(n => n.id === id);
    
    if (notification) {
      notification.progress = progress;
      if (message) notification.message = message;
      
      this.notificationsSubject.next([...notifications]);
      this.updateToastProgress(id, progress);
    }
  }

  /**
   * Complete loading notification
   */
  completeLoading(id: string, title: string = 'Completed', message?: string): void {
    this.hideNotification(id);
    this.showSuccess(title, message, { duration: 3000 });
  }

  /**
   * Show custom notification
   */
  show(notification: Partial<Notification>): string {
    const id = notification.id || this.generateId();
    
    const fullNotification: Notification = {
      id,
      type: 'info',
      title: '',
      duration: 5000,
      persistent: false,
      read: false,
      timestamp: new Date(),
      priority: 'normal',
      ...notification
    };

    this.addNotification(fullNotification);
    this.createToast(fullNotification);

    // Auto-hide if not persistent
    if (!fullNotification.persistent && fullNotification.duration! > 0) {
      timer(fullNotification.duration!).subscribe(() => {
        this.hideNotification(id);
      });
    }

    return id;
  }

  /**
   * Hide specific notification
   */
  hideNotification(id: string): void {
    const notifications = this.notificationsSubject.value.filter(n => n.id !== id);
    const toasts = this.toastsSubject.value.filter(t => t.id !== id);
    
    this.notificationsSubject.next(notifications);
    this.toastsSubject.next(toasts);
    this.updateUnreadCount();
    this.saveNotifications();
  }

  /**
   * Mark notification as read
   */
  markAsRead(id: string): void {
    const notifications = this.notificationsSubject.value.map(n => 
      n.id === id ? { ...n, read: true } : n
    );
    
    this.notificationsSubject.next(notifications);
    this.updateUnreadCount();
    this.saveNotifications();
  }

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): void {
    const notifications = this.notificationsSubject.value.map(n => ({ ...n, read: true }));
    this.notificationsSubject.next(notifications);
    this.unreadCountSubject.next(0);
    this.saveNotifications();
  }

  /**
   * Clear all notifications
   */
  clearAll(): void {
    this.notificationsSubject.next([]);
    this.toastsSubject.next([]);
    this.unreadCountSubject.next(0);
    localStorage.removeItem('sss_notifications');
  }

  // === PRIVATE METHODS ===

  private addNotification(notification: Notification): void {
    const notifications = [notification, ...this.notificationsSubject.value];
    
    // Limit to 100 notifications to prevent memory issues
    if (notifications.length > 100) {
      notifications.splice(100);
    }
    
    this.notificationsSubject.next(notifications);
    this.updateUnreadCount();
    this.saveNotifications();
  }

  private createToast(notification: Notification): void {
    if (notification.type === 'loading' || notification.persistent) {
      return;
    }

    const toast: ToastNotification = {
      id: notification.id,
      type: notification.type,
      title: notification.title,
      message: notification.message,
      duration: notification.duration || 5000,
      persistent: notification.persistent || false,
      actions: notification.actions,
      progress: notification.progress,
      timestamp: notification.timestamp
    };

    const toasts = [toast, ...this.toastsSubject.value];
    this.toastsSubject.next(toasts);

    // Auto-remove toast
    if (!toast.persistent) {
      timer(toast.duration).subscribe(() => {
        this.removeToast(toast.id);
      });
    }
  }

  private removeToast(id: string): void {
    const toasts = this.toastsSubject.value.filter(t => t.id !== id);
    this.toastsSubject.next(toasts);
  }

  private updateToastProgress(id: string, progress: number): void {
    const toasts = this.toastsSubject.value.map(t => 
      t.id === id ? { ...t, progress } : t
    );
    this.toastsSubject.next(toasts);
  }

  private updateUnreadCount(): void {
    const unreadCount = this.notificationsSubject.value.filter(n => !n.read).length;
    this.unreadCountSubject.next(unreadCount);
  }

  private generateId(): string {
    return `notif_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private saveNotifications(): void {
    try {
      const notifications = this.notificationsSubject.value.slice(0, 50);
      localStorage.setItem('sss_notifications', JSON.stringify(notifications));
    } catch (error) {
      console.warn('Failed to save notifications to localStorage:', error);
    }
  }

  private loadStoredNotifications(): void {
    try {
      const stored = localStorage.getItem('sss_notifications');
      if (stored) {
        const notifications: Notification[] = JSON.parse(stored);
        this.notificationsSubject.next(notifications);
        this.updateUnreadCount();
      }
    } catch (error) {
      console.warn('Failed to load notifications from localStorage:', error);
    }
  }
}
