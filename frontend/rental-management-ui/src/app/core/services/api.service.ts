import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getMe() {
    return this.http.get<User>(`${this.base}/users/me`);
  }

  updateMe(payload: Partial<User>) {
    return this.http.patch<User>(`${this.base}/users/me`, payload);
  }

  register(payload: { supabaseUserId: string; firstName: string; lastName: string; email: string }) {
    return this.http.post<{ id: string; role: string }>(`${this.base}/auth/register`, payload);
  }

  inviteTenant(email: string) {
    return this.http.post<{ message: string; token: string; expiresAt: string }>(
      `${this.base}/auth/invite-tenant`, { email }
    );
  }

  acceptInvite(payload: { token: string; firstName: string; lastName: string; supabaseUserId: string }) {
    return this.http.post<{ id: string; role: string }>(`${this.base}/auth/accept-invite`, payload);
  }
}
