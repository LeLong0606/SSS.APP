import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { DashboardComponent } from './dashboard.component';

@NgModule({
  declarations: [
    DashboardComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule
  ],
  providers: [
    DatePipe
  ]
})
export class DashboardModule { }
