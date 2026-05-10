import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Lease } from '../../../core/models/lease.model';
import { LeaseProvision, LeaseProvisionPayload, ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-lease-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, FormsModule],
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

  // Provisions
  provisions: LeaseProvision[] = [];
  provisionsLoading = false;
  savingProvisions = false;
  editingProvisions = false;
  templates: ProvisionTemplate[] = [];
  draftProvisions: { id?: string; title: string; body: string; sortOrder: number }[] = [];

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLease(this.id).subscribe({
      next: (l) => { this.lease = l; this.loading = false; },
      error: () => { this.toast.error('Failed to load lease.'); this.loading = false; }
    });

    if (this.auth.currentUser()?.role === 'Landlord') {
      this.loadReminderSettings();
      this.api.getProvisionTemplates().subscribe({ next: (t) => this.templates = t, error: () => {} });
    }

    this.loadProvisions();
  }

  loadProvisions() {
    this.provisionsLoading = true;
    this.api.getLeaseProvisions(this.id).subscribe({
      next: (p) => { this.provisions = p; this.provisionsLoading = false; },
      error: () => this.provisionsLoading = false
    });
  }

  startEditProvisions() {
    this.draftProvisions = this.provisions.map(p => ({
      id: p.id, title: p.title, body: p.body, sortOrder: p.sortOrder
    }));
    this.editingProvisions = true;
  }

  cancelEditProvisions() {
    this.editingProvisions = false;
    this.draftProvisions = [];
  }

  addDraftProvision() {
    this.draftProvisions.push({ title: '', body: '', sortOrder: this.draftProvisions.length });
  }

  removeDraftProvision(index: number) {
    this.draftProvisions.splice(index, 1);
    this.draftProvisions.forEach((p, i) => p.sortOrder = i);
  }

  moveDraftProvision(index: number, direction: -1 | 1) {
    const swapIdx = index + direction;
    if (swapIdx < 0 || swapIdx >= this.draftProvisions.length) return;
    [this.draftProvisions[index], this.draftProvisions[swapIdx]] =
      [this.draftProvisions[swapIdx], this.draftProvisions[index]];
    this.draftProvisions.forEach((p, i) => p.sortOrder = i);
  }

  addTemplateToProvisions(t: ProvisionTemplate) {
    const alreadyAdded = this.draftProvisions.some(p => p.title === t.title && p.body === t.body);
    if (alreadyAdded) return;
    this.draftProvisions.push({ title: t.title, body: t.body, sortOrder: this.draftProvisions.length });
  }

  saveProvisions() {
    const valid = this.draftProvisions.every(p => p.title.trim() && p.body.trim());
    if (!valid) { this.toast.error('All provisions must have a title and body.'); return; }

    this.savingProvisions = true;
    const payloads: LeaseProvisionPayload[] = this.draftProvisions.map((p, i) => ({
      title: p.title.trim(),
      body: p.body.trim(),
      sortOrder: i
    }));

    this.api.setLeaseProvisions(this.id, payloads).subscribe({
      next: (p) => {
        this.provisions = p;
        this.editingProvisions = false;
        this.draftProvisions = [];
        this.toast.success('Provisions saved.');
      },
      error: () => this.toast.error('Failed to save provisions.'),
      complete: () => setTimeout(() => this.savingProvisions = false)
    });
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
