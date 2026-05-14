import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Property } from '../../../core/models/property.model';
import { ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-lease-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './lease-form.component.html'
})
export class LeaseFormComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  properties: Property[] = [];
  templates: ProvisionTemplate[] = [];
  selectedTemplateIds = new Set<string>();
  oneOffProvisions: { title: string; body: string }[] = [];
  saving = false;

  tenantIdFile: File | null = null;
  tenantIdFileUrl: string | null = null;
  uploadingTenantId = false;

  form: FormGroup = this.fb.group({
    propertyId: ['', Validators.required],
    tenantEmail: ['', [Validators.required, Validators.email]],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    monthlyRent: [null, [Validators.required, Validators.min(1)]],
    depositAmount: [0, [Validators.required, Validators.min(0)]],
    advanceAmount: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit() {
    this.api.getProperties().subscribe({
      next: (props) => this.properties = props.filter(p => !p.isOccupied),
      error: () => this.toast.error('Failed to load properties.')
    });
    this.api.getProvisionTemplates().subscribe({
      next: (t) => this.templates = t,
      error: () => {}
    });
  }

  toggleTemplate(id: string) {
    if (this.selectedTemplateIds.has(id)) {
      this.selectedTemplateIds.delete(id);
    } else {
      this.selectedTemplateIds.add(id);
    }
  }

  addOneOff() {
    this.oneOffProvisions.push({ title: '', body: '' });
  }

  removeOneOff(index: number) {
    this.oneOffProvisions.splice(index, 1);
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    if (this.uploadingTenantId) { this.toast.error('Please wait for the tenant ID upload to finish.'); return; }
    this.saving = true;
    const v = this.form.value;

    this.api.createLease({
      propertyId: v.propertyId,
      tenantEmail: v.tenantEmail,
      startDate: v.startDate,
      endDate: v.endDate,
      monthlyRent: +v.monthlyRent,
      depositAmount: +v.depositAmount,
      advanceAmount: +v.advanceAmount,
      tenantIdFileUrl: this.tenantIdFileUrl || undefined
    }).subscribe({
      next: (lease) => {
        const provisions = this.buildProvisionPayloads();
        if (provisions.length === 0) {
          this.toast.success('Lease created and payments scheduled.');
          this.router.navigate(['/leases', lease.id]);
          return;
        }
        this.api.setLeaseProvisions(lease.id, provisions).subscribe({
          next: () => {
            this.toast.success('Lease created with special provisions.');
            this.router.navigate(['/leases', lease.id]);
          },
          error: () => {
            this.toast.success('Lease created. Failed to save provisions — add them from the lease detail page.');
            this.router.navigate(['/leases', lease.id]);
          }
        });
      },
      error: (err) => {
        const code = err?.error?.code;
        const msg = code === 'PROPERTY_ALREADY_OCCUPIED' ? 'Property already has an active lease.'
                  : code === 'TENANT_NOT_FOUND' ? 'No tenant account found with that email.'
                  : code === 'INVALID_DATE_RANGE' ? 'End date must be after start date.'
                  : 'Failed to create lease.';
        this.toast.error(msg);
        setTimeout(() => this.saving = false);
      }
    });
  }

  onTenantIdSelect(event: Event) {
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

    this.tenantIdFile = file;
    this.tenantIdFileUrl = null;
    this.uploadingTenantId = true;

    const ext = file.name.split('.').pop();
    const path = `tenant-id-${Date.now()}.${ext}`;

    this.api.getUploadUrl('tenant-ids', path).subscribe({
      next: async ({ uploadUrl }) => {
        try {
          const res = await fetch(uploadUrl, {
            method: 'PUT',
            headers: { 'Content-Type': file.type, 'x-upsert': 'true' },
            body: file
          });
          if (!res.ok) throw new Error('Upload failed');
          const validation = await this.api.validateUpload('tenant-ids', path).toPromise();
          if (!validation?.valid) throw new Error('Invalid file type');
          this.tenantIdFileUrl = path;
          this.toast.success('Tenant ID uploaded.');
        } catch {
          this.toast.error('File rejected. Only JPG, PNG, WebP, or PDF files are allowed.');
          this.tenantIdFile = null;
        } finally {
          this.uploadingTenantId = false;
        }
      },
      error: () => {
        this.toast.error('Failed to get upload URL.');
        this.tenantIdFile = null;
        this.uploadingTenantId = false;
      }
    });
  }

  clearTenantId() {
    this.tenantIdFile = null;
    this.tenantIdFileUrl = null;
  }

  private buildProvisionPayloads() {
    const fromTemplates = this.templates
      .filter(t => this.selectedTemplateIds.has(t.id))
      .map((t, idx) => ({ title: t.title, body: t.body, sortOrder: idx }));

    const fromOneOff = this.oneOffProvisions
      .filter(p => p.title.trim() && p.body.trim())
      .map((p, idx) => ({ title: p.title.trim(), body: p.body.trim(), sortOrder: fromTemplates.length + idx }));

    return [...fromTemplates, ...fromOneOff];
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
