import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./register/register.component').then(m => m.RegisterComponent) },
  { path: 'forgot-password', loadComponent: () => import('./forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'accept-invite', loadComponent: () => import('./accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent) },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];
