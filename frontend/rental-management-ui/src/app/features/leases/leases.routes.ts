import { Routes } from '@angular/router';

export const leasesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./leases-list/leases-list.component').then(m => m.LeasesListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./lease-form/lease-form.component').then(m => m.LeaseFormComponent)
  },
  {
    path: ':id/pdf-preview',
    loadComponent: () => import('./lease-pdf-preview/lease-pdf-preview.component').then(m => m.LeasePdfPreviewComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./lease-detail/lease-detail.component').then(m => m.LeaseDetailComponent)
  }
];
