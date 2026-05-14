import { Component, OnInit, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './onboarding-wizard.component.html'
})
export class OnboardingWizardComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  @Output() dismissed = new EventEmitter<boolean>();

  step = signal(1);
  totalSteps = 4;
  saving = false;
  tourStep = signal(0);

  profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', Validators.maxLength(30)]
  });

  tourTooltips = [
    { title: 'Portfolio Overview', body: 'See your total properties, occupied vs vacant, and monthly rent at a glance.' },
    { title: 'Unpaid Rent Tracker', body: 'Track total outstanding rent across all your tenants instantly.' },
    { title: 'Maintenance Monitor', body: 'Stay on top of all open maintenance requests from your tenants.' }
  ];

  ngOnInit() {
    const saved = localStorage.getItem('onboarding_step');
    if (saved) this.step.set(parseInt(saved, 10));

    const user = this.auth.currentUser();
    if (user) {
      this.profileForm.patchValue({
        firstName: user.firstName,
        lastName: user.lastName,
        phone: user.phone ?? ''
      });
    }
  }

  advance() {
    const next = this.step() + 1;
    this.step.set(next);
    localStorage.setItem('onboarding_step', String(next));
  }

  saveProfile() {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.profileForm.value;
    this.api.updateMe({ firstName: v.firstName!, lastName: v.lastName!, phone: v.phone || undefined }).subscribe({
      next: (u) => {
        this.auth.setCurrentUser(u);
        this.saving = false;
        this.advance();
      },
      error: () => {
        this.toast.error('Failed to save profile.');
        this.saving = false;
      }
    });
  }

  goToProperty() {
    localStorage.setItem('onboarding_step', '2');
    this.router.navigate(['/properties/new']);
  }

  goToLease() {
    localStorage.setItem('onboarding_step', '3');
    this.router.navigate(['/leases/new']);
  }

  nextTourStep() {
    const next = this.tourStep() + 1;
    if (next > this.tourTooltips.length) {
      this.completeTour();
    } else {
      this.tourStep.set(next);
    }
  }

  completeTour() {
    this.api.updateMe({ onboardingCompleted: true }).subscribe({
      next: (u) => {
        this.auth.setCurrentUser(u);
        localStorage.removeItem('onboarding_step');
        this.dismissed.emit(true);
      },
      error: () => {
        localStorage.removeItem('onboarding_step');
        this.dismissed.emit(true);
      }
    });
  }

  skip() {
    localStorage.removeItem('onboarding_step');
    this.dismissed.emit(false);
  }

  hasError(field: string) {
    const ctrl = this.profileForm.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
