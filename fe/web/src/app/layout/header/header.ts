import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AdminNotificationService, NotificationLogDto } from '../../core/services/admin-notification.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit, OnDestroy {
  alerts: NotificationLogDto[] = [];
  pollingInterval: any;

  constructor(
    private authService: AuthService, 
    private router: Router,
    private adminNotificationService: AdminNotificationService
  ) {}

  ngOnInit() {
    this.fetchAlerts();
    // Poll every 1 minute
    this.pollingInterval = setInterval(() => {
      this.fetchAlerts();
    }, 60000);
  }

  ngOnDestroy() {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  fetchAlerts() {
    this.adminNotificationService.getUnreadAlerts().subscribe({
      next: (res) => {
        if (res.code >= 200 && res.code < 300) {
          this.alerts = res.data || [];
        }
      }
    });
  }

  acknowledgeAlert(alert: NotificationLogDto, event: Event) {
    event.stopPropagation();
    this.adminNotificationService.acknowledgeAlert(alert.id).subscribe({
      next: (res) => {
        if (res.code >= 200 && res.code < 300) {
          this.alerts = this.alerts.filter(a => a.id !== alert.id);
        }
      }
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
