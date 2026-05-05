import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { createClient, SupabaseClient, Session } from '@supabase/supabase-js';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private supabase: SupabaseClient;
  currentUser = signal<User | null>(null);
  session = signal<Session | null>(null);

  constructor(private router: Router) {
    this.supabase = createClient(environment.supabaseUrl, environment.supabaseAnonKey);

    this.supabase.auth.getSession().then(({ data }) => {
      this.session.set(data.session);
    });

    this.supabase.auth.onAuthStateChange((_, session) => {
      this.session.set(session);
      if (!session) {
        this.currentUser.set(null);
      }
    });
  }

  get accessToken(): string | null {
    return this.session()?.access_token ?? null;
  }

  get isLoggedIn(): boolean {
    return this.session() !== null;
  }

  get role(): string | null {
    return this.currentUser()?.role ?? null;
  }

  async signIn(email: string, password: string) {
    const { error } = await this.supabase.auth.signInWithPassword({ email, password });
    if (error) throw error;
  }

  async signUp(email: string, password: string) {
    const { error } = await this.supabase.auth.signUp({ email, password });
    if (error) throw error;
  }

  async resetPassword(email: string) {
    const { error } = await this.supabase.auth.resetPasswordForEmail(email, {
      redirectTo: `${window.location.origin}/auth/reset-password`
    });
    if (error) throw error;
  }

  async signOut() {
    await this.supabase.auth.signOut();
    this.router.navigate(['/auth/login']);
  }

  setCurrentUser(user: User) {
    this.currentUser.set(user);
  }
}
