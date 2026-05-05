import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { ToastService } from './core/services/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  isLoggedIn = computed(() => this.auth.isLoggedIn);
  user = computed(() => this.auth.currentUser());
  toasts = computed(() => this.toast.toasts());

  signOut() { this.auth.signOut(); }
  dismissToast(id: string) { this.toast.dismiss(id); }
}
