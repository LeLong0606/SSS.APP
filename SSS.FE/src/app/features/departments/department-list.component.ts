import { Component } from '@angular/core';

@Component({
  selector: 'app-department-list',
  template: `
    <div class="page-container">
      <h1>Quản lý phòng ban</h1>
      <p>Chức năng quản lý phòng ban sẽ được triển khai sau.</p>
    </div>
  `,
  styles: [`
    .page-container {
      padding: 2rem;
    }
  `],
  standalone: false
})
export class DepartmentListComponent { }
