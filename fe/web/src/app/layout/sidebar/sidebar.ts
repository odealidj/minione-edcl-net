import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SyncMonitoringService } from '../../core/services/sync-monitoring.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar implements OnInit, OnDestroy {
  authService = inject(AuthService);
  syncService = inject(SyncMonitoringService);
  
  isSyncing = signal(false);
  private syncSub?: Subscription;

  get isAdmin(): boolean {
    const user = this.authService.currentUserValue;
    return user && user.role === 'ADMIN';
  }

  ngOnInit() {
    if (this.isAdmin) {
      this.syncSub = this.syncService.getMetricsStream().subscribe({
        next: (metrics) => {
          this.isSyncing.set(metrics.Status === 'PROCESSING');
        }
      });
    }
  }

  ngOnDestroy() {
    this.syncSub?.unsubscribe();
  }

  menuItems = [
    { title: 'Dashboard', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6', route: '/dashboard' }
  ];

  manifestItem = { title: 'Manifests', icon: 'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z', route: '/manifests' };
  
  masterItems = [
    { title: 'LogisticPartner', route: '/master/logisticPartner' },
    { title: 'GPS Vendor', route: '/master/gps-vendor' },
    { title: 'Route', route: '/master/route' },
    { title: 'Driver', route: '/master/driver' },
    { title: 'Truck', route: '/master/truck' },
    { title: 'Truck Assignments', route: '/master/truck-assignments' },
    { title: 'Supplier', route: '/master/supplier' }
  ];
  
  operationItems = [
    { title: 'DCL Monitoring', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z', route: '/operations/dcl' },
    { title: 'Delivery Monitoring', icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6', route: '/operations/monitoring' },
    { title: 'IDCS Deliveries', icon: 'M20.25 7.151V2.25h-2.25v3.424l-3-1.636L12 2.25l-3 1.636-3-1.636L3 3.886v16.228l3-1.636 3 1.636 3-1.636 3 1.636 3-1.636 3 1.636V7.151z', route: '/admin/idcs-deliveries' },
    { title: 'Route Planning', icon: 'M9 6.75V15m6-6v8.25m.503 3.498l4.875-2.437c.381-.19.622-.58.622-1.006V4.82c0-.836-.88-1.38-1.628-1.006l-3.869 1.934c-.317.159-.69.159-1.006 0L9.503 3.252a1.125 1.125 0 00-1.006 0L3.622 5.689C3.24 5.88 3 6.27 3 6.695V19.18c0 .836.88 1.38 1.628 1.006l3.869-1.934c.317-.159.69-.159 1.006 0l4.994 2.497c.317.158.69.158 1.006 0z', route: '/admin/route-planning' },
  ];
}
