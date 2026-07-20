import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminNotificationService, NotificationLogDto } from '../../../core/services/admin-notification.service';

@Component({
  selector: 'app-notification-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notification-logs.html'
})
export class NotificationLogsComponent implements OnInit {
  logs: NotificationLogDto[] = [];
  loading = false;
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  
  // Filters
  q = '';
  isRead: string = ''; // '' = all, 'true' = Read, 'false' = Unread
  deliveryStatus = ''; // '' = all
  driverId: number | null = null;
  drivers: any[] = []; // Using any[] to bypass DriverDto for now if not needed heavily

  constructor(
    private notificationService: AdminNotificationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // skip loadDrivers since we don't have driver service imported?
    // wait, I can just fetch drivers manually if needed or remove the driver dropdown.
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.cdr.detectChanges();
    this.notificationService.getNotificationLogs(
      this.currentPage, 
      this.pageSize, 
      this.driverId || undefined, 
      this.deliveryStatus || undefined,
      this.q || undefined,
      this.isRead || undefined
    ).subscribe({
      next: (res) => {
        if (res.code >= 200 && res.code < 300) {
          this.logs = res.data || [];
          this.totalItems = res.pagination?.total_items || 0;
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('API Error GetNotificationLogs:', err);
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onFilterChange(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadLogs();
  }

  resendNotification(id: number): void {
    if (!confirm('Are you sure you want to resend this notification to the driver?')) return;
    
    this.notificationService.resendNotification(id).subscribe({
      next: (res) => {
        if (res.code >= 200 && res.code < 300) {
          alert('Notification resent successfully!');
          this.loadLogs();
        } else {
          alert('Failed to resend notification');
        }
      },
      error: (err) => {
        alert('Failed to resend notification: ' + err.message);
      }
    });
  }
}
