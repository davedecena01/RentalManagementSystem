import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-provision-templates',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './provision-templates.component.html'
})
export class ProvisionTemplatesComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  templates: ProvisionTemplate[] = [];
  loading = true;
  saving = false;
  editingId: string | null = null;

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getProvisionTemplates().subscribe({
      next: (t) => { this.templates = t; this.loading = false; },
      error: () => { this.toast.error('Failed to load templates.'); this.loading = false; }
    });
  }

  startEdit(t: ProvisionTemplate) {
    this.editingId = t.id;
    this.form.patchValue({ title: t.title, body: t.body });
  }

  cancelEdit() {
    this.editingId = null;
    this.form.reset();
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.form.value;
    const req = this.editingId
      ? this.api.updateProvisionTemplate(this.editingId, v)
      : this.api.createProvisionTemplate(v);

    req.subscribe({
      next: () => {
        this.toast.success(this.editingId ? 'Template updated.' : 'Template saved.');
        this.editingId = null;
        this.form.reset();
        this.load();
      },
      error: () => this.toast.error('Failed to save template.'),
      complete: () => setTimeout(() => this.saving = false)
    });
  }

  delete(id: string) {
    if (!confirm('Delete this provision template? Existing lease provisions are not affected.')) return;
    this.api.deleteProvisionTemplate(id).subscribe({
      next: () => { this.toast.success('Template deleted.'); this.load(); },
      error: () => this.toast.error('Failed to delete template.')
    });
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
