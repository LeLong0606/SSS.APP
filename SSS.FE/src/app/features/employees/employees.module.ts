import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { EmployeesRoutingModule } from './employees-routing.module';

// Components
import { EmployeeListComponent } from './employee-list.component';
import { EmployeeFormComponent } from './employee-form.component';
import { EmployeeDetailComponent } from './employee-detail.component';

@NgModule({
  declarations: [
    EmployeeListComponent,
    EmployeeFormComponent,
    EmployeeDetailComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule,
    EmployeesRoutingModule
  ]
})
export class EmployeesModule { }
