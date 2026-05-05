import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
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
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatch });
  }

  private passwordMatch(group: FormGroup) {
    const pw = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return pw === confirm ? null : { passwordMismatch: true };
  }

  async onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;

    try {
      const supabaseUserId = await this.auth.signUp(this.form.value.email, this.form.value.password);
      this.api.register({
        supabaseUserId,
        firstName: this.form.value.firstName,
        lastName: this.form.value.lastName,
        email: this.form.value.email
      }).subscribe({
        next: () => {
          this.toast.success('Account created! Please check your email to confirm your address.');
          this.router.navigate(['/auth/login']);
        },
        error: () => {
          this.toast.info('Account created. Please verify your email before logging in.');
          this.router.navigate(['/auth/login']);
        }
      });
    } catch (err: any) {
      this.toast.error(err.message ?? 'Registration failed.');
    } finally {
      setTimeout(() => this.loading = false);
    }
  }

  get firstName() { return this.form.get('firstName')!; }
  get lastName() { return this.form.get('lastName')!; }
  get email() { return this.form.get('email')!; }
  get password() { return this.form.get('password')!; }
  get confirmPassword() { return this.form.get('confirmPassword')!; }
}
