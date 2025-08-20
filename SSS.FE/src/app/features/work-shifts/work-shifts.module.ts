import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { WorkShiftListComponent } from './work-shift-list.component';
import { WorkShiftFormComponent } from './work-shift-form.component';
import { WorkShiftDetailComponent } from './work-shift-detail.component';

@NgModule({
  declarations: [
    WorkShiftListComponent,
    WorkShiftFormComponent,
    WorkShiftDetailComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forChild([
      { path: '', component: WorkShiftListComponent },
      { path: 'create', component: WorkShiftFormComponent },
      { path: ':id', component: WorkShiftDetailComponent },
      { path: ':id/edit', component: WorkShiftFormComponent }
    ])
  ]
})
export class WorkShiftsModule { }
