import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  authService = inject(AuthService);

  get isAdmin(): boolean {
    const user = this.authService.currentUserValue;
    return user && user.role === 'ADMIN';
  }

  menuItems = [
    { title: 'Dashboard', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6', route: '/dashboard' },
  ];
  
  masterItems = [

    { title: 'Driver', route: '/master/driver' },
    { title: 'Supplier', route: '/master/supplier' },
    { title: 'Truck', route: '/master/truck' },
    { title: 'Manifest', route: '/master/manifest' }

  ];
  
  operationItems = [
    { title: 'DCL Monitoring', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z', route: '/operations/dcl' },
    { title: 'Delivery Monitoring', icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6', route: '/operations/monitoring' },
  ];
}
