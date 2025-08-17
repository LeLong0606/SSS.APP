import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { EmployeeListComponent } from './employee-list.component';
import { EmployeeFormComponent } from './employee-form.component';
import { EmployeeDetailComponent } from './employee-detail.component';

import { AuthGuard } from '../../core/guards/auth.guard';
import { RoleGuard } from '../../core/guards/role.guard';

const routes: Routes = [
  {
    path: '',
    component: EmployeeListComponent,
    canActivate: [AuthGuard],
    data: {
      title: 'Danh sách nhân viên',
      requiredRoles: ['Employee', 'TeamLeader', 'Director', 'Administrator']
    }
  },
  {
    path: 'create',
    component: EmployeeFormComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: {
      title: 'Thêm nhân viên mới',
      requiredRoles: ['TeamLeader', 'Director', 'Administrator']
    }
  },
  {
    path: ':id',
    component: EmployeeDetailComponent,
    canActivate: [AuthGuard],
    data: {
      title: 'Chi tiết nhân viên',
      requiredRoles: ['Employee', 'TeamLeader', 'Director', 'Administrator']
    }
  },
  {
    path: ':id/edit',
    component: EmployeeFormComponent,
    canActivate: [AuthGuard, RoleGuard],
    data: {
      title: 'Chỉnh sửa nhân viên',
      requiredRoles: ['TeamLeader', 'Director', 'Administrator']
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmployeesRoutingModule { }
