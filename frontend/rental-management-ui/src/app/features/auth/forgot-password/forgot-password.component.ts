import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  form: FormGroup;
  loading = false;
  sent = false;

  constructor(private fb: FormBuilder, private auth: AuthService, private toast: ToastService) {
    this.form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  }

  async onSubmit() {
    if (this.form.invalid) return;
    this.loading = true;
    try {
      await this.auth.resetPassword(this.form.value.email);
      this.sent = true;
    } catch (err: any) {
      this.toast.error(err.message ?? 'Failed to send reset email.');
    } finally {
      setTimeout(() => this.loading = false);
    }
  }

  get email() { return this.form.get('email')!; }
}
