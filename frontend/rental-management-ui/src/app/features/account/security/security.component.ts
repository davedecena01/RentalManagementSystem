import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

type MfaStep = 'idle' | 'verifying';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './security.component.html'
})
export class SecurityComponent implements OnInit {
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private sanitizer = inject(DomSanitizer);

  loading = true;
  saving = false;
  step: MfaStep = 'idle';

  enrolledFactors: { id: string; status: string; factorType: string }[] = [];
  pendingFactorId = '';
  qrCode: SafeHtml | string = '';
  secret = '';
  verifyCode = '';

  get isMfaEnabled(): boolean {
    return this.enrolledFactors.some(f => f.status === 'verified');
  }

  async ngOnInit() {
    try {
      this.enrolledFactors = await this.auth.getMfaFactors();
    } catch {
      this.toast.error('Failed to load security settings.');
    } finally {
      this.loading = false;
    }
  }

  async startEnroll() {
    this.saving = true;
    try {
      const result = await this.auth.enrollMfa();
      this.pendingFactorId = result.id;
      this.qrCode = this.sanitizer.bypassSecurityTrustHtml(result.qrCode);
      this.secret = result.secret;
      this.step = 'verifying';
    } catch {
      this.toast.error('Failed to start MFA enrollment.');
    } finally {
      this.saving = false;
    }
  }

  async verify() {
    if (!this.verifyCode.trim()) return;
    this.saving = true;
    try {
      await this.auth.verifyMfaEnrollment(this.pendingFactorId, this.verifyCode.trim());
      this.toast.success('Two-factor authentication enabled.');
      this.enrolledFactors = await this.auth.getMfaFactors();
      this.step = 'idle';
      this.verifyCode = '';
      this.qrCode = '';
    } catch {
      this.toast.error('Invalid code. Please try again.');
    } finally {
      this.saving = false;
    }
  }

  cancelEnroll() {
    if (this.pendingFactorId) {
      this.auth.unenrollMfa(this.pendingFactorId).catch(() => {});
    }
    this.step = 'idle';
    this.qrCode = '';
    this.secret = '';
    this.verifyCode = '';
    this.pendingFactorId = '';
  }

  async unenroll() {
    const factor = this.enrolledFactors.find(f => f.status === 'verified');
    if (!factor) return;
    if (!confirm('Disable two-factor authentication? Your account will be less secure.')) return;
    this.saving = true;
    try {
      await this.auth.unenrollMfa(factor.id);
      this.toast.success('Two-factor authentication disabled.');
      this.enrolledFactors = await this.auth.getMfaFactors();
    } catch {
      this.toast.error('Failed to disable MFA.');
    } finally {
      this.saving = false;
    }
  }
}
