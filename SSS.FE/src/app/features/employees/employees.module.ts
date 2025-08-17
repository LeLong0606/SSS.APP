import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { EmployeeListComponent } from './employee-list.component';

@NgModule({
  declarations: [
    EmployeeListComponent
  ],
  imports: [
    CommonModule,
    RouterModule.forChild([
      { path: '', component: EmployeeListComponent }
    ])
  ]
})
export class EmployeesModule { }
