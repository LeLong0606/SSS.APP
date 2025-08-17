import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';

// Layout components
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

// Core imports
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

@NgModule({
  declarations: [
    App,
    MainLayoutComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    RouterModule,
    AppRoutingModule
  ],
  providers: [
    // Use the new HTTP client provider (Angular 15+)
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor])
    )
  ],
  bootstrap: [App]
})
export class AppModule { }
