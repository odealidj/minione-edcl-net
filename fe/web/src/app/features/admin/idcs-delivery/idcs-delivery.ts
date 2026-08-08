import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CargoService } from '../../../core/services/cargo.service';
import { SearchBarComponent } from '../../../shared/components/search-bar/search-bar.component';
import { finalize, tap } from 'rxjs/operators';

@Component({
  selector: 'app-idcs-delivery',
  standalone: true,
  imports: [CommonModule, SearchBarComponent],
  templateUrl: './idcs-delivery.html'
})
export class IdcsDeliveryComponent implements OnInit {
  private cargoService = inject(CargoService);
  private cdr = inject(ChangeDetectorRef);

  deliveries: any[] = [];
  isLoading = false;
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalRecords = 0;
  totalPages = 1;

  searchQuery = '';

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading = true;
    console.log('Sending request to getIdcsDeliveries');
    this.cargoService.getIdcsDeliveries(this.searchQuery, this.currentPage, this.pageSize)
      .pipe(
        tap(res => console.log('Response received in tap:', res)),
        finalize(() => {
          console.log('Observable finalized');
          this.isLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res) => {
          this.deliveries = res.data || [];
          if (res.pagination) {
            this.currentPage = res.pagination.page;
            this.totalRecords = res.pagination.total_items;
            this.totalPages = res.pagination.total_pages;
          }
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
        }
      });
  }

  onSearch(query: string) {
    this.searchQuery = query;
    this.currentPage = 1;
    this.loadData();
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadData();
    }
  }

  get pages(): number[] {
    const pages = [];
    for (let i = 1; i <= this.totalPages; i++) {
      pages.push(i);
    }
    return pages;
  }
}
