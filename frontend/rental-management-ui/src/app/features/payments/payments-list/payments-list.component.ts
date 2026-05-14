import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Payment } from '../../../core/models/payment.model';

@Component({
  selector: 'app-payments-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
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

  proofFile: File | null = null;
  proofFileUrl: string | null = null;
  uploadingProof = false;
  loadingProof: Record<string, boolean> = {};

  manualForm: FormGroup = this.fb.group({
    amountPaid: [null, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  ngOnInit() {
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
    this.proofFile = null;
    this.proofFileUrl = null;
    this.manualForm.reset({ amountPaid: p.amountDue - p.amountPaid, notes: '' });
    this.showManualPayModal = true;
  }

  closeManualPay() {
    this.showManualPayModal = false;
    this.selectedPayment = null;
    this.proofFile = null;
    this.proofFileUrl = null;
  }

  onFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const allowed = ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];
    if (!allowed.includes(file.type)) {
      this.toast.error('Only JPG, PNG, WebP, or PDF files are allowed.');
      input.value = '';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('File must be under 5 MB.');
      input.value = '';
      return;
    }

    this.proofFile = file;
    this.proofFileUrl = null;
    this.uploadProof(file);
  }

  clearProof() {
    this.proofFile = null;
    this.proofFileUrl = null;
  }

  private uploadProof(file: File) {
    if (!this.selectedPayment) return;
    this.uploadingProof = true;
    const ext = file.name.split('.').pop();
    const path = `${this.selectedPayment.id}/${Date.now()}.${ext}`;

    this.api.getUploadUrl('payment-proofs', path).subscribe({
      next: async ({ uploadUrl }) => {
        try {
          const res = await fetch(uploadUrl, {
            method: 'PUT',
            headers: { 'Content-Type': file.type, 'x-upsert': 'true' },
            body: file
          });
          if (!res.ok) throw new Error('Upload failed');
          this.proofFileUrl = path;
          this.toast.success('Proof uploaded.');
        } catch {
          this.toast.error('Failed to upload proof file.');
          this.proofFile = null;
        } finally {
          this.uploadingProof = false;
        }
      },
      error: () => {
        this.toast.error('Failed to get upload URL.');
        this.uploadingProof = false;
        this.proofFile = null;
      }
    });
  }

  submitManualPay() {
    if (this.manualForm.invalid || !this.selectedPayment) { this.manualForm.markAllAsTouched(); return; }
    if (this.uploadingProof) { this.toast.error('Please wait for the file upload to finish.'); return; }
    this.savingPay = true;
    const v = this.manualForm.value;

    this.api.manualPay(this.selectedPayment.id, {
      amountPaid: +v.amountPaid,
      notes: v.notes || undefined,
      proofFileUrl: this.proofFileUrl || undefined
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

  viewPaymentProof(p: Payment) {
    if (!p.proofFileUrl) return;
    this.loadingProof[p.id] = true;
    this.api.getSignedUrl('payment-proofs', p.proofFileUrl).subscribe({
      next: ({ url }) => {
        window.open(url, '_blank');
        this.loadingProof[p.id] = false;
      },
      error: () => {
        this.toast.error('Could not load payment proof. Please try again.');
        this.loadingProof[p.id] = false;
      }
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
