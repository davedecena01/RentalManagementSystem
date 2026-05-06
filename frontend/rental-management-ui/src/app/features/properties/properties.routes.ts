import { Routes } from '@angular/router';

export const propertiesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./properties-list/properties-list.component').then(m => m.PropertiesListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./property-detail/property-detail.component').then(m => m.PropertyDetailComponent)
  }
];
