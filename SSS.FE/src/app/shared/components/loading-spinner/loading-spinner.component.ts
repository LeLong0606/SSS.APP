import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  template: `
    <div class="loading-spinner-container" [class.overlay]="overlay">
      <div class="loading-spinner" [class]="'spinner-' + size">
        <div class="spinner-ring" *ngFor="let ring of rings"></div>
      </div>
      <div class="loading-text" *ngIf="text">{{ text }}</div>
    </div>
  `,
  styleUrls: ['./loading-spinner.component.scss'],
  standalone: false
})
export class LoadingSpinnerComponent {
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() text: string = '';
  @Input() overlay: boolean = false;
  
  rings = [1, 2, 3]; // Number of rings in spinner
}
