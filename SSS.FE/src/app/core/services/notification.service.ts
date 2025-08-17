import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface NotificationMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title?: string;
  message: string;
  duration?: number;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notificationsSubject = new BehaviorSubject<NotificationMessage[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  private defaultDuration = 5000; // 5 seconds

  constructor() {}

  // Show success notification
  showSuccess(message: string, title?: string, duration?: number): void {
    this.addNotification('success', message, title, duration);
  }

  // Show error notification
  showError(message: string, title?: string, duration?: number): void {
    this.addNotification('error', message, title, duration || 0); // Errors don't auto-dismiss by default
  }

  // Show warning notification
  showWarning(message: string, title?: string, duration?: number): void {
    this.addNotification('warning', message, title, duration);
  }

  // Show info notification
  showInfo(message: string, title?: string, duration?: number): void {
    this.addNotification('info', message, title, duration);
  }

  // Remove specific notification
  removeNotification(id: string): void {
    const currentNotifications = this.notificationsSubject.value;
    const updatedNotifications = currentNotifications.filter(n => n.id !== id);
    this.notificationsSubject.next(updatedNotifications);
  }

  // Clear all notifications
  clearAll(): void {
    this.notificationsSubject.next([]);
  }

  // Get current notifications
  getNotifications(): NotificationMessage[] {
    return this.notificationsSubject.value;
  }

  private addNotification(
    type: NotificationMessage['type'],
    message: string,
    title?: string,
    duration?: number
  ): void {
    const notification: NotificationMessage = {
      id: this.generateId(),
      type,
      title,
      message,
      duration: duration ?? this.defaultDuration,
      timestamp: new Date()
    };

    const currentNotifications = this.notificationsSubject.value;
    this.notificationsSubject.next([...currentNotifications, notification]);

    // Auto-remove notification after duration (if duration > 0)
    const finalDuration = notification.duration ?? 0;
    if (finalDuration > 0) {
      setTimeout(() => {
        this.removeNotification(notification.id);
      }, finalDuration);
    }
  }

  private generateId(): string {
    return 'notification-' + Date.now() + '-' + Math.random().toString(36).substring(2, 9);
  }
}
