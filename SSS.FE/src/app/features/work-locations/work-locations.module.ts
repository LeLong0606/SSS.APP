import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

import { WorkLocationListComponent } from './work-location-list.component';
import { WorkLocationFormComponent } from './work-location-form.component';
import { WorkLocationDetailComponent } from './work-location-detail.component';

const routes: Routes = [
  {
    path: '',
    component: WorkLocationListComponent
  },
  {
    path: 'create',
    component: WorkLocationFormComponent
  },
  {
    path: ':id',
    component: WorkLocationDetailComponent
  },
  {
    path: ':id/edit',
    component: WorkLocationFormComponent
  }
];

@NgModule({
  declarations: [
    WorkLocationListComponent,
    WorkLocationFormComponent,
    WorkLocationDetailComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes)
  ]
})
export class WorkLocationsModule { }
