import { Routes } from '@angular/router';
import { authGuard, landlordGuard } from '../../core/guards/auth.guard';

export const accountRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./profile/profile.component')
        .then(m => m.ProfileComponent)
  },
  {
    path: 'provision-templates',
    canActivate: [landlordGuard],
    loadComponent: () =>
      import('./provision-templates/provision-templates.component')
        .then(m => m.ProvisionTemplatesComponent)
  }
];
