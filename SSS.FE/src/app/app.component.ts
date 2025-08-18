import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-container" [attr.data-theme]="currentTheme">
      <router-outlet></router-outlet>
      
      <!-- Toast Container -->
      <app-toast-container></app-toast-container>
      
      <!-- Loading Overlay for app-wide loading states -->
      <app-loading-spinner 
        *ngIf="isAppLoading" 
        [overlay]="true"
        text="Đang khởi tạo ứng dụng...">
      </app-loading-spinner>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      transition: all var(--transition-normal);
    }
    
    // Smooth theme transition
    [data-theme="dark"] {
      --bg-primary: #0f172a;
      --bg-secondary: #1e293b;
      --text-primary: #e2e8f0;
      --text-secondary: #cbd5e1;
      --border-primary: #334155;
    }
  `],
  standalone: false
})
export class AppComponent implements OnInit {
  title = 'SSS Employee Management';
  currentTheme = 'light';
  isAppLoading = false;

  constructor(
    private router: Router,
    private authService: AuthService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.initializeApp();
    this.setupRouterLoading();
    this.loadUserTheme();
  }

  private initializeApp(): void {
    this.isAppLoading = true;
    
    // Initialize auth state and other app-wide services
    Promise.all([
      this.authService.getCurrentUser().toPromise().catch(() => null),
      // Add other initialization tasks here
    ]).then(() => {
      this.isAppLoading = false;
      this.showWelcomeNotification();
    }).catch(() => {
      this.isAppLoading = false;
    });
  }

  private setupRouterLoading(): void {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        // Add page transition effects or analytics here
        this.trackPageView(event.url);
      });
  }

  private loadUserTheme(): void {
    const savedTheme = localStorage.getItem('sss-theme');
    if (savedTheme) {
      this.currentTheme = savedTheme;
    } else {
      // Detect system preference
      if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        this.currentTheme = 'dark';
      }
    }
  }

  private showWelcomeNotification(): void {
    const user = this.authService.getCurrentUserSync();
    if (user) {
      this.notificationService.showSuccess(
        'Chào mừng trở lại!',
        `Xin chào ${user.fullName}. Hệ thống đã sẵn sàng.`,
        { duration: 3000 }
      );
    }
  }

  private trackPageView(url: string): void {
    // Analytics tracking can be added here
    console.log('Page view:', url);
  }
}
