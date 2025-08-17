import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { WorkShiftListComponent } from './work-shift-list.component';

@NgModule({
  declarations: [
    WorkShiftListComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild([
      { path: '', component: WorkShiftListComponent }
    ])
  ]
})
export class WorkShiftsModule { }
