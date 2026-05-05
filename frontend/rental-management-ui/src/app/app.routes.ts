import { Routes } from '@angular/router';
import { authGuard, landlordGuard, tenantGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },

  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes)
  },

  {
    path: 'properties',
    canActivate: [landlordGuard],
    loadChildren: () => import('./features/properties/properties.routes').then(m => m.propertiesRoutes)
  },

  {
    path: 'leases',
    canActivate: [landlordGuard],
    loadChildren: () => import('./features/leases/leases.routes').then(m => m.leasesRoutes)
  },

  {
    path: 'payments',
    canActivate: [authGuard],
    loadChildren: () => import('./features/payments/payments.routes').then(m => m.paymentsRoutes)
  },

  {
    path: 'maintenance',
    canActivate: [authGuard],
    loadChildren: () => import('./features/maintenance/maintenance.routes').then(m => m.maintenanceRoutes)
  },

  {
    path: 'account',
    canActivate: [authGuard],
    loadChildren: () => import('./features/account/account.routes').then(m => m.accountRoutes)
  },

  { path: '**', redirectTo: 'dashboard' }
];
