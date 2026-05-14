import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AppLog } from '../../../core/models/log.model';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './activity.component.html'
})
export class ActivityComponent implements OnInit {
  private api = inject(ApiService);

  logs: AppLog[] = [];
  loading = true;
  totalCount = 0;
  page = 1;
  readonly pageSize = 20;
  selectedType = '';

  readonly entityTypes = [
    { value: '', label: 'All' },
    { value: 'Property', label: 'Properties' },
    { value: 'Lease', label: 'Leases' },
    { value: 'Payment', label: 'Payments' },
    { value: 'Maintenance', label: 'Maintenance' }
  ];

  ngOnInit() { this.loadLogs(true); }

  loadLogs(reset = false) {
    if (reset) { this.page = 1; this.logs = []; }
    this.loading = true;
    this.api.getLogs(this.selectedType || undefined, this.page, this.pageSize).subscribe({
      next: (r) => {
        this.logs = reset ? r.items : [...this.logs, ...r.items];
        this.totalCount = r.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  filterByType(value: string) {
    this.selectedType = value;
    this.loadLogs(true);
  }

  loadMore() {
    this.page++;
    this.loadLogs(false);
  }

  get hasMore(): boolean {
    return this.logs.length < this.totalCount;
  }

  relativeTime(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${Math.max(1, mins)}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }

  iconFor(entityType: string): string {
    const icons: Record<string, string> = {
      Property: '🏠', Lease: '📄', Payment: '💰', Maintenance: '🔧', Tenant: '👤'
    };
    return icons[entityType] ?? '📝';
  }
}
