import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-dashboard',
  template: `
    <div class="page-container">
      <h1>Quản trị hệ thống</h1>
      <p>Chức năng quản trị hệ thống sẽ được triển khai sau.</p>
    </div>
  `,
  styles: [`
    .page-container {
      padding: 2rem;
    }
  `],
  standalone: false
})
export class AdminDashboardComponent { }
