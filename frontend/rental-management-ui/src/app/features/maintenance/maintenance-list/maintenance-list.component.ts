import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { MaintenanceRequest } from '../../../core/models/maintenance.model';

@Component({
  selector: 'app-maintenance-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule],
  templateUrl: './maintenance-list.component.html'
})
export class MaintenanceListComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);

  requests: MaintenanceRequest[] = [];
  loading = true;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  showResolveModal = false;
  selectedRequest: MaintenanceRequest | null = null;
  savingResolve = false;

  resolveForm: FormGroup = this.fb.group({
    resolutionNotes: ['', Validators.maxLength(1000)]
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.api.getMaintenanceRequests().subscribe({
      next: (data) => { this.requests = data; this.loading = false; },
      error: () => { this.toast.error('Failed to load maintenance requests.'); this.loading = false; }
    });
  }

  /** Count of open / in-progress requests for hero stats. */
  openCount() { return this.requests.filter(r => r.status !== 'Resolved').length; }
  /** Count of resolved requests for hero stats. */
  resolvedCount() { return this.requests.filter(r => r.status === 'Resolved').length; }

  openResolve(r: MaintenanceRequest) {
    this.selectedRequest = r;
    this.resolveForm.reset({ resolutionNotes: '' });
    this.showResolveModal = true;
  }

  closeResolve() {
    this.showResolveModal = false;
    this.selectedRequest = null;
  }

  submitResolve() {
    if (!this.selectedRequest) return;
    this.savingResolve = true;
    const notes = this.resolveForm.value.resolutionNotes || undefined;

    this.api.resolveMaintenanceRequest(this.selectedRequest.id, notes).subscribe({
      next: () => {
        this.toast.success('Request marked as resolved.');
        this.closeResolve();
        this.load();
      },
      error: () => this.toast.error('Failed to resolve request.'),
      complete: () => setTimeout(() => this.savingResolve = false)
    });
  }

  priorityClass(priority: string) {
    return priority === 'High' ? 'badge-red' : priority === 'Medium' ? 'badge-yellow' : 'badge-blue';
  }

  statusClass(status: string) {
    return status === 'Resolved' ? 'badge-green' : status === 'InProgress' ? 'badge-yellow' : 'badge-red';
  }

  statusLabel(status: string) {
    return status === 'InProgress' ? 'In Progress' : status;
  }
}
