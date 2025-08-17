import { Component } from '@angular/core';

@Component({
  selector: 'app-profile',
  template: `
    <div class="page-container">
      <h1>Hồ sơ cá nhân</h1>
      <p>Chức năng quản lý hồ sơ cá nhân sẽ được triển khai sau.</p>
    </div>
  `,
  styles: [`
    .page-container {
      padding: 2rem;
    }
  `],
  standalone: false
})
export class ProfileComponent { }
