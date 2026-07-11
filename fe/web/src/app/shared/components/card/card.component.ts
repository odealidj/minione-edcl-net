import { Component } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  template: `
    <div class="overflow-x-auto bg-base-100 rounded-box shadow">
      <ng-content></ng-content>
    </div>
  `
})
export class CardComponent {}
