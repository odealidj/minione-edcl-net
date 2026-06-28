import { Routes } from '@angular/router';
import { MainLayout } from './layout/main-layout/main-layout';
import { Login } from './features/auth/login/login';
import { Dashboard } from './features/dashboard/dashboard';
import { Driver } from './features/master/driver/driver';
import { Supplier } from './features/master/supplier/supplier';
import { Monitoring } from './features/operations/monitoring/monitoring';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'master/driver', component: Driver },
      { path: 'master/supplier', component: Supplier },
      { path: 'operations/monitoring', component: Monitoring },
      // Fallback for sub-routes
      { path: '**', redirectTo: 'dashboard' }
    ]
  }
];
