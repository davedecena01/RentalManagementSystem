import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Lease } from '../../../core/models/lease.model';

@Component({
  selector: 'app-leases-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './leases-list.component.html'
})
export class LeasesListComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);

  leases: Lease[] = [];
  loading = true;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.api.getLeases().subscribe({
      next: (data) => { this.leases = data; this.loading = false; },
      error: () => { this.toast.error('Failed to load leases.'); this.loading = false; }
    });
  }

  terminate(lease: Lease) {
    if (!confirm(`Terminate lease for "${lease.propertyName}"? This cannot be undone.`)) return;
    this.api.terminateLease(lease.id).subscribe({
      next: () => { this.toast.success('Lease terminated.'); this.load(); },
      error: () => this.toast.error('Failed to terminate lease.')
    });
  }

  statusClass(status: string) {
    return status === 'Active' ? 'badge-green' : status === 'Terminated' ? 'badge-red' : 'badge-yellow';
  }

  formatDate(d: string) {
    return new Date(d).toLocaleDateString('en-PH', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  formatMoney(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }
}
