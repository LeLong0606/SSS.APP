import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, provideHttpClient, withInterceptors } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// Core imports
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

// Interceptors
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

// Guards
import { AuthGuard } from './core/guards/auth.guard';
import { NoAuthGuard } from './core/guards/no-auth.guard';
import { RoleGuard } from './core/guards/role.guard';

// Core Services
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { EmployeeService } from './core/services/employee.service';
import { DepartmentService } from './core/services/department.service';
import { WorkShiftService } from './core/services/work-shift.service';
import { WorkLocationService } from './core/services/work-location.service';

// Layout Components
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

// Shared Components
import { ToastContainerComponent } from './shared/components/toast-container/toast-container.component';
import { LoadingSpinnerComponent } from './shared/components/loading-spinner/loading-spinner.component';
import { ConfirmDialogComponent } from './shared/components/confirm-dialog/confirm-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    MainLayoutComponent,
    ToastContainerComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent
  ],
  imports: [
    // Angular Core Modules
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    
    // App Routing - MUST BE LAST
    AppRoutingModule
  ],
  providers: [
    // ✅ FIX: Use the new provideHttpClient with functional interceptors
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor])
    ),
    
    // Route Guards
    AuthGuard,
    NoAuthGuard,
    RoleGuard,
    
    // Services (with @Injectable({ providedIn: 'root' }))
    AuthService,
    NotificationService,
    EmployeeService,
    DepartmentService,
    WorkShiftService,
    WorkLocationService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
