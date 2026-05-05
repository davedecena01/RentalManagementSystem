import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  form: FormGroup;
  loading = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private api: ApiService,
    private toast: ToastService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  async onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;

    try {
      await this.auth.signIn(this.form.value.email, this.form.value.password);
      this.api.getMe().subscribe({
        next: (user) => {
          this.auth.setCurrentUser(user);
          this.router.navigate(['/dashboard']);
        },
        error: () => this.router.navigate(['/dashboard'])
      });
    } catch (err: any) {
      this.toast.error(err.message ?? 'Login failed. Please check your credentials.');
    } finally {
      setTimeout(() => this.loading = false);
    }
  }

  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }
}
