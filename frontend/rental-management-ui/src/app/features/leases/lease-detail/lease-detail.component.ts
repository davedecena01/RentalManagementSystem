import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Lease } from '../../../core/models/lease.model';

@Component({
  selector: 'app-lease-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './lease-detail.component.html'
})
export class LeaseDetailComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  lease: Lease | null = null;
  loading = true;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  reminderLoading = false;
  savingReminder = false;

  reminderForm: FormGroup = this.fb.group({
    isEnabled: [true],
    daysBeforeDue: [3, [Validators.required, Validators.min(0), Validators.max(30)]],
    daysAfterDue: [1, [Validators.required, Validators.min(0), Validators.max(30)]]
  });

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLease(this.id).subscribe({
      next: (l) => { this.lease = l; this.loading = false; },
      error: () => { this.toast.error('Failed to load lease.'); this.loading = false; }
    });

    if (this.auth.currentUser()?.role === 'Landlord') {
      this.loadReminderSettings();
    }
  }

  loadReminderSettings() {
    this.reminderLoading = true;
    this.api.getReminderSettings(this.id).subscribe({
      next: (s) => {
        this.reminderForm.patchValue({
          isEnabled: s.isEnabled,
          daysBeforeDue: s.daysBeforeDue,
          daysAfterDue: s.daysAfterDue
        });
        this.reminderLoading = false;
      },
      error: () => { this.reminderLoading = false; }
    });
  }

  saveReminders() {
    if (this.reminderForm.invalid) return;
    this.savingReminder = true;
    this.api.updateReminderSettings(this.id, this.reminderForm.value).subscribe({
      next: () => { this.toast.success('Reminder settings saved.'); },
      error: () => { this.toast.error('Failed to save reminder settings.'); },
      complete: () => setTimeout(() => this.savingReminder = false)
    });
  }

  statusClass(s: string) {
    return s === 'Active' ? 'badge-green' : s === 'Terminated' ? 'badge-red' : 'badge-yellow';
  }

  formatDate(d: string) {
    return new Date(d).toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' });
  }

  formatMoney(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }
}
