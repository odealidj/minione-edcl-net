import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-search-input',
  standalone: true,
  imports: [FormsModule, CommonModule],
  template: `
    <div class="relative">
      <svg xmlns="http://www.w3.org/2000/svg" class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <input type="text" [(ngModel)]="searchTerm" 
             class="input input-sm input-bordered w-full pl-9 pr-8 bg-base-100" 
             [placeholder]="placeholder" 
             (keyup.enter)="onSearch()" />
      @if (searchTerm) {
        <button class="btn btn-ghost btn-xs btn-circle absolute right-1 top-1/2 -translate-y-1/2 text-base-content/40 hover:text-base-content/80" 
                (click)="clearSearch()">
          ✕
        </button>
      }
    </div>
  `
})
export class SearchInputComponent {
  @Input() placeholder: string = 'Search...';
  @Output() search = new EventEmitter<string>();
  
  searchTerm: string = '';

  onSearch() {
    this.search.emit(this.searchTerm);
  }

  clearSearch() {
    this.searchTerm = '';
    this.search.emit('');
  }
}
