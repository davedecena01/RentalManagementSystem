import { Routes } from '@angular/router';
import { landlordGuard } from '../../core/guards/auth.guard';

export const accountRoutes: Routes = [
  {
    path: 'provision-templates',
    canActivate: [landlordGuard],
    loadComponent: () =>
      import('./provision-templates/provision-templates.component')
        .then(m => m.ProvisionTemplatesComponent)
  }
];
