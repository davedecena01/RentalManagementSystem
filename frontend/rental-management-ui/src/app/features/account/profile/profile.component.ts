import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  user: User | null = null;
  loading = true;
  saving = false;
  editing = false;

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', Validators.maxLength(30)]
  });

  ngOnInit() {
    this.api.getMe().subscribe({
      next: (u) => {
        this.user = u;
        this.loading = false;
        this.form.patchValue({ firstName: u.firstName, lastName: u.lastName, phone: u.phone ?? '' });
      },
      error: () => { this.toast.error('Failed to load profile.'); this.loading = false; }
    });
  }

  startEdit() { this.editing = true; }

  cancelEdit() {
    this.editing = false;
    if (this.user) {
      this.form.patchValue({ firstName: this.user.firstName, lastName: this.user.lastName, phone: this.user.phone ?? '' });
    }
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.form.value;
    this.api.updateMe({ firstName: v.firstName!, lastName: v.lastName!, phone: v.phone || undefined }).subscribe({
      next: (u) => {
        this.user = u;
        this.editing = false;
        this.toast.success('Profile updated.');
      },
      error: () => this.toast.error('Failed to update profile.'),
      complete: () => setTimeout(() => this.saving = false)
    });
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }

  formatDate(d: string) {
    return new Date(d).toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' });
  }

  /** Two-letter user initials for the avatar circle in the profile card. */
  initials(): string {
    if (!this.user) return '';
    const f = (this.user.firstName || '').trim();
    const l = (this.user.lastName || '').trim();
    if (!f && !l) return (this.user.email || '?').slice(0, 1).toUpperCase();
    return (f.charAt(0) + l.charAt(0)).toUpperCase();
  }
}
