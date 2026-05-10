import { Routes } from '@angular/router';

export const accountRoutes: Routes = [
  {
    path: 'provision-templates',
    loadComponent: () =>
      import('./provision-templates/provision-templates.component')
        .then(m => m.ProvisionTemplatesComponent)
  }
];
