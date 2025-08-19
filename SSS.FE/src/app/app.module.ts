import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

// Core Services and Interceptors
import {
  AuthInterceptor,
  LoadingInterceptor,
  AuthService,
  EmployeeService,
  DepartmentService,
  WorkLocationService,
  WorkShiftService,
  ImageService,
  AttendanceService,
  ShiftManagementService,
  PayrollService,
  LoadingService,
  NotificationService
} from './core/services';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    ReactiveFormsModule,
    FormsModule,
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
      useClass: LoadingInterceptor,
      multi: true
    },
    
    // Core Services
    AuthService,
    EmployeeService,
    DepartmentService,
    WorkLocationService,
    WorkShiftService,
    ImageService,
    AttendanceService,
    ShiftManagementService,
    PayrollService,
    LoadingService,
    NotificationService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
