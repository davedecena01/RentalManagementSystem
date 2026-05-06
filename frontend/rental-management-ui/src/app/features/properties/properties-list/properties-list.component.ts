import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Property } from '../../../core/models/property.model';

@Component({
  selector: 'app-properties-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './properties-list.component.html'
})
export class PropertiesListComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  properties: Property[] = [];
  loading = true;
  showForm = false;
  saving = false;
  editTarget: Property | null = null;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    address: ['', [Validators.required, Validators.maxLength(500)]],
    type: ['Apartment', Validators.required],
    description: ['']
  });

  readonly propertyTypes = ['Apartment', 'House', 'Condo', 'Commercial'];

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.api.getProperties().subscribe({
      next: (data) => { this.properties = data; this.loading = false; },
      error: () => { this.toast.error('Failed to load properties.'); this.loading = false; }
    });
  }

  openCreate() {
    this.editTarget = null;
    this.form.reset({ type: 'Apartment' });
    this.showForm = true;
  }

  openEdit(p: Property) {
    this.editTarget = p;
    this.form.patchValue({ name: p.name, address: p.address, type: p.type, description: p.description ?? '' });
    this.showForm = true;
  }

  closeForm() {
    this.showForm = false;
    this.editTarget = null;
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const payload = this.form.value;

    const obs = this.editTarget
      ? this.api.updateProperty(this.editTarget.id, payload)
      : this.api.createProperty(payload);

    obs.subscribe({
      next: () => {
        this.toast.success(this.editTarget ? 'Property updated.' : 'Property created.');
        this.closeForm();
        this.load();
      },
      error: () => {
        this.toast.error('Failed to save property.');
        setTimeout(() => this.saving = false);
      },
      complete: () => setTimeout(() => this.saving = false)
    });
  }

  delete(p: Property) {
    if (!confirm(`Delete "${p.name}"? This cannot be undone.`)) return;
    this.api.deleteProperty(p.id).subscribe({
      next: () => { this.toast.success('Property deleted.'); this.load(); },
      error: () => this.toast.error('Failed to delete property.')
    });
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
