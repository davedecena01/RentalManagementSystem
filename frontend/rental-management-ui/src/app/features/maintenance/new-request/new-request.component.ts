import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { environment } from '../../../../environments/environment';

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

  photoFile: File | null = null;
  photoUrl: string | null = null;
  uploadingPhoto = false;

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

  onPhotoSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) {
      this.toast.error('Only JPG, PNG, or WebP images are allowed.');
      input.value = '';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('File must be under 5 MB.');
      input.value = '';
      return;
    }

    this.photoFile = file;
    this.photoUrl = null;
    this.uploadingPhoto = true;

    const ext = file.name.split('.').pop();
    const path = `maintenance-${Date.now()}.${ext}`;

    this.api.getUploadUrl('maintenance-images', path).subscribe({
      next: async ({ uploadUrl }) => {
        try {
          const res = await fetch(uploadUrl, {
            method: 'PUT',
            headers: { 'Content-Type': file.type, 'x-upsert': 'true' },
            body: file
          });
          if (!res.ok) throw new Error('Upload failed');
          this.photoUrl = `${environment.supabaseUrl}/storage/v1/object/public/maintenance-images/${path}`;
          this.toast.success('Photo uploaded.');
        } catch {
          this.toast.error('Failed to upload photo.');
          this.photoFile = null;
        } finally {
          this.uploadingPhoto = false;
        }
      },
      error: () => {
        this.toast.error('Failed to get upload URL.');
        this.photoFile = null;
        this.uploadingPhoto = false;
      }
    });
  }

  clearPhoto() {
    this.photoFile = null;
    this.photoUrl = null;
  }

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    if (this.uploadingPhoto) { this.toast.error('Please wait for the photo upload to finish.'); return; }
    this.loading = true;
    const v = this.form.value;

    this.api.createMaintenanceRequest({
      propertyId: v.propertyId!,
      title: v.title!,
      description: v.description!,
      priority: v.priority!,
      imageUrl: this.photoUrl || undefined
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
