import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';

interface PropertyOption { id: string; name: string; }

@Component({
  selector: 'app-new-request',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './new-request.component.html'
})
export class NewRequestComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  properties: PropertyOption[] = [];
  loading = false;

  form = this.fb.group({
    propertyId: ['', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    priority: ['Medium', Validators.required]
  });

  ngOnInit() {
    this.api.getLeases().subscribe({
      next: (leases) => {
        const active = leases.filter(l => l.status === 'Active');
        this.properties = active.map(l => ({ id: l.propertyId, name: l.propertyName }));
        if (this.properties.length === 1) {
          this.form.patchValue({ propertyId: this.properties[0].id });
        }
      },
      error: () => this.toast.error('Failed to load lease information.')
    });
  }

  get propertyId() { return this.form.get('propertyId')!; }
  get title() { return this.form.get('title')!; }
  get description() { return this.form.get('description')!; }

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    const v = this.form.value;

    this.api.createMaintenanceRequest({
      propertyId: v.propertyId!,
      title: v.title!,
      description: v.description!,
      priority: v.priority!
    }).subscribe({
      next: () => {
        this.toast.success('Maintenance request submitted.');
        this.router.navigate(['/maintenance']);
      },
      error: (err) => {
        const code = err?.error?.code;
        if (code === 'NO_ACTIVE_LEASE') {
          this.toast.error('You must have an active lease for this property.');
        } else {
          this.toast.error('Failed to submit request.');
        }
      },
      complete: () => setTimeout(() => this.loading = false)
    });
  }
}
