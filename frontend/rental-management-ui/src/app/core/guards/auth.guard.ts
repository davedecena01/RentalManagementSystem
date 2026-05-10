import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ApiService } from '../services/api.service';

async function resolveUser(auth: AuthService, api: ApiService) {
  await auth.sessionReady;
  if (auth.isLoggedIn && !auth.currentUser()) {
    try {
      const user = await firstValueFrom(api.getMe());
      auth.setCurrentUser(user);
    } catch {
      // user fetch failed — let guard decide based on session alone
    }
  }
}

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const api = inject(ApiService);
  const router = inject(Router);

  await resolveUser(auth, api);
  if (auth.isLoggedIn) return true;
  return router.createUrlTree(['/auth/login']);
};

export const landlordGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const api = inject(ApiService);
  const router = inject(Router);

  await resolveUser(auth, api);
  if (auth.isLoggedIn && auth.role === 'Landlord') return true;
  return router.createUrlTree(['/auth/login']);
};

export const tenantGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const api = inject(ApiService);
  const router = inject(Router);

  await resolveUser(auth, api);
  if (auth.isLoggedIn && auth.role === 'Tenant') return true;
  return router.createUrlTree(['/auth/login']);
};
