import { NgModule } from '@angular/core';
import { RouterModule, Routes, PreloadAllModules } from '@angular/router';

import { AuthGuard } from './core/guards/auth.guard';
import { NoAuthGuard } from './core/guards/no-auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { UserRole } from './core/models/auth.model';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

const routes: Routes = [
  // Default redirect
  { 
    path: '', 
    redirectTo: '/dashboard', 
    pathMatch: 'full' 
  },

  // Authentication routes (public) - no layout wrapper
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule),
    canActivate: [NoAuthGuard]
  },

  // Protected routes with main layout
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      // Dashboard (protected - all authenticated users)
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
        data: { 
          roles: [UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
        }
      },

      // Employee management (protected - TeamLeader+)
      {
        path: 'employees',
        loadChildren: () => import('./features/employees/employees.module').then(m => m.EmployeesModule),
        canActivate: [RoleGuard],
        data: { 
          roles: [UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
        }
      },

      // Department management (protected - Director+)
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/departments.module').then(m => m.DepartmentsModule),
        canActivate: [RoleGuard],
        data: { 
          roles: [UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
        }
      },

      // Work shifts (protected - Employee+)
      {
        path: 'work-shifts',
        loadChildren: () => import('./features/work-shifts/work-shifts.module').then(m => m.WorkShiftsModule),
        canActivate: [RoleGuard],
        data: { 
          roles: [UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.DIRECTOR, UserRole.ADMINISTRATOR]
        }
      },

      // User profile (protected - all authenticated users)
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule)
      },

      // System administration (protected - Administrator only)
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule),
        canActivate: [RoleGuard],
        data: { 
          roles: [UserRole.ADMINISTRATOR]
        }
      }
    ]
  },

  // 404 Page
  {
    path: '404',
    loadChildren: () => import('./shared/components/not-found/not-found.module').then(m => m.NotFoundModule)
  },

  // Wildcard route - must be last
  { 
    path: '**', 
    redirectTo: '/404' 
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    // Enable router preloading for better performance
    preloadingStrategy: PreloadAllModules,
    
    // Enable tracing for debugging (disable in production)
    enableTracing: false,
    
    // Scroll to top on route change
    scrollPositionRestoration: 'top'
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
