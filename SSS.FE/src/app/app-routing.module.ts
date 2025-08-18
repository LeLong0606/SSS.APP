import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Guards
import { AuthGuard } from './core/guards/auth.guard';
import { NoAuthGuard } from './core/guards/no-auth.guard';

// Layout Components
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

const routes: Routes = [
  // Default redirect
  { 
    path: '', 
    redirectTo: '/dashboard', 
    pathMatch: 'full' 
  },
  
  // Public routes (Authentication)
  {
    path: 'auth',
    canActivate: [NoAuthGuard],
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  
  // Protected routes with MainLayout
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule)
      },
      {
        path: 'employees',
        loadChildren: () => import('./features/employees/employees.module').then(m => m.EmployeesModule)
      },
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/departments.module').then(m => m.DepartmentsModule)
      },
      {
        path: 'work-shifts',
        loadChildren: () => import('./features/work-shifts/work-shifts.module').then(m => m.WorkShiftsModule)
      },
      {
        path: 'work-locations',
        loadChildren: () => import('./features/work-locations/work-locations.module').then(m => m.WorkLocationsModule)
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule)
      }
    ]
  },
  
  // Wildcard route - must be last
  { 
    path: '**', 
    redirectTo: '/dashboard' 
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    enableTracing: false, // Set to true for debugging
    useHash: false,
    scrollPositionRestoration: 'top'
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
