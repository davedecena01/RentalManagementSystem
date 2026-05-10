import { Routes } from '@angular/router';

export const paymentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./payments-list/payments-list.component').then(m => m.PaymentsListComponent)
  },
  {
    path: ':id/receipt-preview',
    loadComponent: () => import('./receipt-preview/receipt-preview.component').then(m => m.ReceiptPreviewComponent)
  }
];
