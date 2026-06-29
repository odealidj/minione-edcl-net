import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { Dashboard } from './features/dashboard/dashboard';
import { DriverComponent } from './features/master/driver/driver';
import { SupplierComponent } from './features/master/supplier/supplier';
import { Monitoring } from './features/operations/monitoring/monitoring';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

import { TransporterComponent } from './features/master/transporter/transporter';
import { TruckComponent } from './features/master/truck/truck';
import { ManifestComponent } from './features/master/manifest/manifest';


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

      { path: 'master/transporter', component: TransporterComponent, canActivate: [adminGuard] },
      { path: 'master/truck', component: TruckComponent },
      { path: 'master/manifest', component: ManifestComponent },

      { path: 'operations/monitoring', component: Monitoring },
      { path: 'admin/users', loadComponent: () => import('./features/admin/user-management/user-management').then(c => c.UserManagementComponent), canActivate: [adminGuard] },
      // Fallback for sub-routes
      { path: '**', redirectTo: 'dashboard' }
    ]
  }
];
