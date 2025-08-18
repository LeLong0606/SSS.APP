import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

// External Libraries
import { NgChartsModule } from 'ng2-charts';

// Core imports
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';

// Interceptors
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { ErrorInterceptor } from './core/interceptors/error.interceptor';

// Guards
import { AuthGuard } from './core/guards/auth.guard';
import { NoAuthGuard } from './core/guards/no-auth.guard';
import { RoleGuard } from './core/guards/role.guard';

// Services
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

// Feature Modules (Lazy loaded modules will be imported through routing)

@NgModule({
  declarations: [
    AppComponent,
    MainLayoutComponent,
    ToastContainerComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent
  ],
  imports: [
    // Angular Core
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    
    // External Libraries
    NgChartsModule,
    
    // App Routing (should be last)
    AppRoutingModule
  ],
  providers: [
    // HTTP Interceptors
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    },
    
    // Guards
    AuthGuard,
    NoAuthGuard,
    RoleGuard,
    
    // Core Services
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
