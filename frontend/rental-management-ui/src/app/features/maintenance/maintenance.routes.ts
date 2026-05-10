import { Routes } from '@angular/router';
import { tenantGuard } from '../../core/guards/auth.guard';

export const maintenanceRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./maintenance-list/maintenance-list.component').then(m => m.MaintenanceListComponent)
  },
  {
    path: 'new',
    canActivate: [tenantGuard],
    loadComponent: () => import('./new-request/new-request.component').then(m => m.NewRequestComponent)
  }
];
