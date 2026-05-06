import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Payment } from '../../../core/models/payment.model';

@Component({
  selector: 'app-payments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './payments-list.component.html'
})
export class PaymentsListComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  payments: Payment[] = [];
  loading = true;
  statusFilter = '';
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  showManualPayModal = false;
  selectedPayment: Payment | null = null;
  savingPay = false;

  manualForm: FormGroup = this.fb.group({
    amountPaid: [null, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  ngOnInit() {
    // Handle Stripe return
    const sessionId = this.route.snapshot.queryParamMap.get('session_id');
    if (sessionId) {
      this.toast.success('Payment submitted via Stripe. Status will update shortly.');
    }
    this.load();
  }

  load() {
    this.loading = true;
    const filter = this.statusFilter || undefined;
    this.api.getPayments(filter).subscribe({
      next: (data) => { this.payments = data; this.loading = false; },
      error: () => { this.toast.error('Failed to load payments.'); this.loading = false; }
    });
  }

  payWithStripe(p: Payment) {
    this.api.createStripeCheckout(p.leaseId).subscribe({
      next: (res) => window.location.href = res.checkoutUrl,
      error: () => this.toast.error('Failed to initiate Stripe checkout.')
    });
  }

  openManualPay(p: Payment) {
    this.selectedPayment = p;
    this.manualForm.reset({ amountPaid: p.amountDue - p.amountPaid, notes: '' });
    this.showManualPayModal = true;
  }

  closeManualPay() {
    this.showManualPayModal = false;
    this.selectedPayment = null;
  }

  submitManualPay() {
    if (this.manualForm.invalid || !this.selectedPayment) { this.manualForm.markAllAsTouched(); return; }
    this.savingPay = true;
    const v = this.manualForm.value;

    this.api.manualPay(this.selectedPayment.id, {
      amountPaid: +v.amountPaid,
      notes: v.notes || undefined
    }).subscribe({
      next: () => {
        this.toast.success('Payment recorded.');
        this.closeManualPay();
        this.load();
      },
      error: () => this.toast.error('Failed to record payment.'),
      complete: () => setTimeout(() => this.savingPay = false)
    });
  }

  statusClass(status: string) {
    return status === 'Paid' ? 'badge-green' : status === 'Partial' ? 'badge-yellow' : 'badge-red';
  }

  formatDate(d: string) {
    return new Date(d + 'T00:00:00').toLocaleDateString('en-PH', { year: 'numeric', month: 'short' });
  }

  formatMoney(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }

  hasError(field: string) {
    const ctrl = this.manualForm.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
