import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginationMeta } from '../../../core/models/api.model';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="meta && meta.total_items > 0" class="flex flex-col sm:flex-row justify-between items-center mt-4 gap-4">
      <div class="flex items-center gap-4 text-sm text-base-content/70">
        <span>Showing {{ currentItemsCount }} of {{ meta.total_items }} entries</span>
        <div class="flex items-center gap-2">
          <span>Rows per page:</span>
          <select 
            class="select select-bordered select-sm w-20" 
            [ngModel]="meta.page_size" 
            (ngModelChange)="onPageSizeChange($event)">
            <option *ngFor="let size of pageSizeOptions" [value]="size">{{ size }}</option>
          </select>
        </div>
      </div>
      <div class="join" *ngIf="meta.total_pages > 1">
        <button class="join-item btn btn-sm" 
                [disabled]="!meta.has_previous" 
                (click)="onPageChange(meta.prevPage!)">«</button>
        <button class="join-item btn btn-sm">Page {{ meta.page }} of {{ meta.total_pages }}</button>
        <button class="join-item btn btn-sm" 
                [disabled]="!meta.has_next" 
                (click)="onPageChange(meta.nextPage!)">»</button>
      </div>
    </div>
  `
})
export class PaginationComponent {
  @Input({ required: true }) meta!: PaginationMeta | null;
  @Input({ required: true }) currentItemsCount!: number;
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];
  
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  onPageChange(page: number) {
    if (page) {
      this.pageChange.emit(page);
    }
  }

  onPageSizeChange(size: number) {
    // Make sure we emit a number, not string from HTML select
    this.pageSizeChange.emit(Number(size));
  }
}
