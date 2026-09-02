import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { Dashboard } from './features/dashboard/dashboard';
import { DriverComponent } from './features/master/driver/driver';
import { SupplierComponent } from './features/master/supplier/supplier';

import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

import { LogisticPartnerComponent } from './features/master/logistic-partner/logistic-partner';
import { TruckComponent } from './features/master/truck/truck';
import { ManifestComponent } from './features/master/manifest/manifest';
import { TruckAssignment } from './features/master/truck-assignment/truck-assignment';


export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'master/driver', component: DriverComponent },
      { path: 'master/supplier', component: SupplierComponent },

      { path: 'manifests', component: ManifestComponent },

      { path: 'master/logisticPartner', component: LogisticPartnerComponent, canActivate: [adminGuard] },
      { path: 'master/gps-vendor', loadComponent: () => import('./features/master/gps-vendor/gps-vendor').then(c => c.GpsVendorComponent), canActivate: [adminGuard] },
      { path: 'master/route', loadComponent: () => import('./features/master/route/route').then(c => c.RouteComponent), canActivate: [adminGuard] },
      { path: 'master/truck', component: TruckComponent },
      { path: 'master/truck-assignments', component: TruckAssignment },

      { path: 'operations/monitoring', loadComponent: () => import('./features/admin/delivery-monitoring/delivery-monitoring').then(c => c.DeliveryMonitoringComponent) },
      { path: 'admin/users', loadComponent: () => import('./features/admin/user-management/user-management').then(c => c.UserManagementComponent), canActivate: [adminGuard] },
      { path: 'admin/background-jobs', loadComponent: () => import('./features/admin/background-jobs/background-jobs').then(c => c.BackgroundJobsComponent), canActivate: [adminGuard] },
      { path: 'admin/notification-logs', loadComponent: () => import('./features/admin/notification-logs/notification-logs').then(c => c.NotificationLogsComponent), canActivate: [adminGuard] },
      { path: 'admin/manifest-problems', loadComponent: () => import('./features/admin/manifest-problem/manifest-problem').then(c => c.ManifestProblemComponent), canActivate: [adminGuard] },
      { path: 'admin/route-planning', loadComponent: () => import('./features/admin/route-planning/route-planning/route-planning').then(c => c.RoutePlanningComponent), canActivate: [adminGuard] },
      { path: 'admin/route-planning/:id', loadComponent: () => import('./features/admin/route-planning/route-planning-form/route-planning-form').then(c => c.RoutePlanningFormComponent), canActivate: [adminGuard] },
      { path: 'admin/sync-monitoring', loadComponent: () => import('./features/admin/sync-monitoring/sync-monitoring').then(c => c.SyncMonitoring), canActivate: [adminGuard] },
      { path: 'admin/idcs-deliveries', loadComponent: () => import('./features/admin/idcs-delivery/idcs-delivery').then(c => c.IdcsDeliveryComponent), canActivate: [adminGuard] },
      { path: 'admin/system-observability', loadComponent: () => import('./features/admin/system-observability/system-observability').then(c => c.SystemObservabilityComponent), canActivate: [adminGuard] },
      // Fallback for sub-routes
      { path: '**', redirectTo: 'dashboard' }
    ]
  }
];
