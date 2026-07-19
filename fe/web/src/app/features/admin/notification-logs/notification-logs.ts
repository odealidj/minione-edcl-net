import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminNotificationService, NotificationLogDto } from '../../../core/services/admin-notification.service';
import { PaginationMeta } from '../../../core/models/api.model';

@Component({
  selector: 'app-notification-logs',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './notification-logs.html',
})
export class NotificationLogsComponent implements OnInit {
  logs: NotificationLogDto[] = [];
  pagination: PaginationMeta = { page: 1, page_size: 10, total_pages: 0, total_items: 0, has_previous: false, has_next: false, nextPage: null, prevPage: null };
  
  loading: boolean = false;
  
  filterDriverId: number | null = null;
  filterStatus: string = '';

  constructor(
    private notificationService: AdminNotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.cdr.detectChanges();
    
    const statusFilter = this.filterStatus === '' ? undefined : this.filterStatus;
    const driverFilter = this.filterDriverId ? this.filterDriverId : undefined;

    this.notificationService.getNotificationLogs(this.pagination.page, this.pagination.page_size, driverFilter, statusFilter).subscribe({
      next: (res) => {
        this.logs = res.data || [];
        if (res.pagination) {
          this.pagination = res.pagination;
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Failed to load notification logs:", err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    this.pagination.page = 1;
    this.loadData();
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.pagination.total_pages) {
      this.pagination.page = newPage;
      this.loadData();
    }
  }

  clearFilters() {
    this.filterDriverId = null;
    this.filterStatus = '';
    this.applyFilters();
  }
}
