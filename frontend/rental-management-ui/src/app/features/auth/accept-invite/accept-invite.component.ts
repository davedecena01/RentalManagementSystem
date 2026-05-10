import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './accept-invite.component.html'
})
export class AcceptInviteComponent implements OnInit {
  form: FormGroup;
  loading = false;
  token = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private auth: AuthService,
    private api: ApiService,
    private toast: ToastService,
    private router: Router
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatch });
  }

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) {
      this.toast.error('Invalid invite link.');
      this.router.navigate(['/auth/login']);
    }
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
      await this.auth.signUp(this.form.value.email, this.form.value.password);
      const session = this.auth.session();
      if (!session) throw new Error('Session not available after signup.');

      this.api.acceptInvite({
        token: this.token,
        firstName: this.form.value.firstName,
        lastName: this.form.value.lastName,
        supabaseUserId: session.user.id
      }).subscribe({
        next: () => {
          this.toast.success('Welcome! Your tenant account is ready.');
          this.router.navigate(['/dashboard']);
        },
        error: (err) => this.toast.error(err.error?.error ?? 'Failed to accept invite.')
      });
    } catch (err: any) {
      this.toast.error(err.message ?? 'Something went wrong.');
    } finally {
      setTimeout(() => this.loading = false);
    }
  }

  get email() { return this.form.get('email')!; }
  get firstName() { return this.form.get('firstName')!; }
  get lastName() { return this.form.get('lastName')!; }
  get password() { return this.form.get('password')!; }
  get confirmPassword() { return this.form.get('confirmPassword')!; }
}
