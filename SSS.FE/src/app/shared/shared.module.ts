import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

// Shared Components
import { ToastContainerComponent } from './components/toast-container/toast-container.component';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';

@NgModule({
  declarations: [
    ToastContainerComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule
  ],
  exports: [
    // Modules
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    
    // Components
    ToastContainerComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent
  ]
})
export class SharedModule { }
