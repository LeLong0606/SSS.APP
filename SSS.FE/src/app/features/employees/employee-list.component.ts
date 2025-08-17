import { Component } from '@angular/core';

@Component({
  selector: 'app-employee-list',
  template: `
    <div class="page-container">
      <h1>Quản lý nhân viên</h1>
      <p>Chức năng quản lý nhân viên sẽ được triển khai sau.</p>
    </div>
  `,
  styles: [`
    .page-container {
      padding: 2rem;
    }
  `],
  standalone: false
})
export class EmployeeListComponent { }
