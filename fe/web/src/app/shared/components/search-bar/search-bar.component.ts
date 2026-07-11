import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SearchInputComponent } from '../search-input/search-input.component';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [SearchInputComponent],
  template: `
    <div class="p-4 border-b border-base-200 flex justify-between items-center bg-base-200/20">
      <app-search-input [placeholder]="placeholder" (search)="search.emit($event)"></app-search-input>
      <ng-content></ng-content>
    </div>
  `
})
export class SearchBarComponent {
  @Input() placeholder: string = 'Search...';
  @Output() search = new EventEmitter<string>();
}
