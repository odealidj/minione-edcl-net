import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4">
      <h1 class="text-2xl font-bold text-base-content">{{ title }}</h1>
      <div class="flex flex-wrap gap-2">
        <ng-content></ng-content>
        <button *ngIf="buttonText" class="btn btn-primary" (click)="actionClick.emit()" [disabled]="disabled">
          <svg *ngIf="buttonIcon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5 mr-2">
            <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="buttonIcon" />
          </svg>
          <svg *ngIf="!buttonIcon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5 mr-2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          {{ buttonText }}
        </button>
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() buttonText?: string;
  @Input() buttonIcon?: string;
  @Input() disabled: boolean = false;
  
  @Output() actionClick = new EventEmitter<void>();
}
