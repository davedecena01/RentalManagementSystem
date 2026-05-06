import { Routes } from '@angular/router';

export const paymentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./payments-list/payments-list.component').then(m => m.PaymentsListComponent)
  }
];
