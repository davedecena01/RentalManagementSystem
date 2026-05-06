import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Property } from '../../../core/models/property.model';

@Component({
  selector: 'app-lease-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './lease-form.component.html'
})
export class LeaseFormComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  properties: Property[] = [];
  saving = false;

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
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.form.value;

    this.api.createLease({
      propertyId: v.propertyId,
      tenantEmail: v.tenantEmail,
      startDate: v.startDate,
      endDate: v.endDate,
      monthlyRent: +v.monthlyRent,
      depositAmount: +v.depositAmount,
      advanceAmount: +v.advanceAmount
    }).subscribe({
      next: (lease) => {
        this.toast.success('Lease created and payments scheduled.');
        this.router.navigate(['/leases', lease.id]);
      },
      error: (err) => {
        const code = err?.error?.code;
        const msg = code === 'PROPERTY_ALREADY_OCCUPIED' ? 'Property already has an active lease.'
                  : code === 'TENANT_NOT_FOUND' ? 'No tenant account found with that email.'
                  : code === 'INVALID_DATE_RANGE' ? 'End date must be after start date.'
                  : 'Failed to create lease.';
        this.toast.error(msg);
        setTimeout(() => this.saving = false);
      },
      complete: () => setTimeout(() => this.saving = false)
    });
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
