import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { DepartmentListComponent } from './department-list.component';

@NgModule({
  declarations: [
    DepartmentListComponent
  ],
  imports: [
    CommonModule,
    RouterModule.forChild([
      { path: '', component: DepartmentListComponent }
    ])
  ]
})
export class DepartmentsModule { }
