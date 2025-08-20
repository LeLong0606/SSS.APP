import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { DepartmentListComponent } from './department-list.component';
import { DepartmentFormComponent } from './department-form.component';
import { DepartmentDetailComponent } from './department-detail.component';

@NgModule({
  declarations: [
    DepartmentListComponent,
    DepartmentFormComponent,
    DepartmentDetailComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forChild([
      { path: '', component: DepartmentListComponent },
      { path: 'create', component: DepartmentFormComponent },
      { path: ':id', component: DepartmentDetailComponent },
      { path: ':id/edit', component: DepartmentFormComponent }
    ])
  ]
})
export class DepartmentsModule { }
