import { Component, OnInit, AfterViewInit, OnDestroy, ViewChild, ElementRef, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { DashboardData } from '../../core/models/dashboard.model';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('incomeChart') incomeCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('breakdownChart') breakdownCanvasRef!: ElementRef<HTMLCanvasElement>;

  private auth = inject(AuthService);
  private api = inject(ApiService);

  user = this.auth.currentUser;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  data: DashboardData | null = null;
  loading = true;

  private incomeChart: Chart | null = null;
  private breakdownChart: Chart | null = null;
  private chartsReady = false;
  private dataReady = false;

  ngOnInit() {
    if (!this.auth.currentUser()) {
      this.api.getMe().subscribe(user => this.auth.setCurrentUser(user));
    }
    this.api.getDashboard().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.dataReady = true;
        if (this.chartsReady) this.renderCharts();
      },
      error: () => { this.loading = false; }
    });
  }

  ngAfterViewInit() {
    this.chartsReady = true;
    if (this.dataReady) this.renderCharts();
  }

  ngOnDestroy() {
    this.incomeChart?.destroy();
    this.breakdownChart?.destroy();
  }

  private renderCharts() {
    if (!this.data) return;
    this.renderIncomeChart();
    this.renderBreakdownChart();
  }

  private renderIncomeChart() {
    if (!this.incomeCanvasRef || !this.data?.monthlyIncome.length) return;
    this.incomeChart?.destroy();
    const labels = this.data.monthlyIncome.map(p => {
      const [y, m] = p.month.split('-');
      return new Date(+y, +m - 1).toLocaleDateString('en-PH', { month: 'short', year: 'numeric' });
    });
    this.incomeChart = new Chart(this.incomeCanvasRef.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Income (₱)',
          data: this.data.monthlyIncome.map(p => p.amount),
          backgroundColor: 'rgba(99, 102, 241, 0.7)',
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { callback: v => '₱' + Number(v).toLocaleString() } } }
      }
    });
  }

  private renderBreakdownChart() {
    if (!this.breakdownCanvasRef || !this.data) return;
    const { paid, partial, unpaid } = this.data.paymentBreakdown;
    if (paid + partial + unpaid === 0) return;
    this.breakdownChart?.destroy();
    this.breakdownChart = new Chart(this.breakdownCanvasRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Paid', 'Partial', 'Unpaid'],
        datasets: [{
          data: [paid, partial, unpaid],
          backgroundColor: ['#22c55e', '#f59e0b', '#ef4444'],
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
      }
    });
  }

  fmt(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }
}
