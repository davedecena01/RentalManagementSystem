import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Lease } from '../../../core/models/lease.model';

@Component({
  selector: 'app-lease-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './lease-detail.component.html'
})
export class LeaseDetailComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  lease: Lease | null = null;
  loading = true;

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLease(this.id).subscribe({
      next: (l) => { this.lease = l; this.loading = false; },
      error: () => { this.toast.error('Failed to load lease.'); this.loading = false; }
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
