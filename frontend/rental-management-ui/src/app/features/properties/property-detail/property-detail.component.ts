import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Property, InventoryItem } from '../../../core/models/property.model';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './property-detail.component.html'
})
export class PropertyDetailComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  property: Property | null = null;
  inventory: InventoryItem[] = [];
  loading = true;
  showItemForm = false;
  savingItem = false;
  editItem: InventoryItem | null = null;

  itemForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    condition: ['Good', Validators.required]
  });

  readonly conditions = ['Good', 'Fair', 'Poor'];

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.loadProperty();
    this.loadInventory();
  }

  loadProperty() {
    this.api.getProperty(this.id).subscribe({
      next: (p) => { this.property = p; this.loading = false; },
      error: () => { this.toast.error('Failed to load property.'); this.loading = false; }
    });
  }

  loadInventory() {
    this.api.getInventory(this.id).subscribe({
      next: (items) => this.inventory = items,
      error: () => this.toast.error('Failed to load inventory.')
    });
  }

  openAddItem() {
    this.editItem = null;
    this.itemForm.reset({ condition: 'Good' });
    this.showItemForm = true;
  }

  openEditItem(item: InventoryItem) {
    this.editItem = item;
    this.itemForm.patchValue({ name: item.name, description: item.description ?? '', condition: item.condition });
    this.showItemForm = true;
  }

  closeItemForm() {
    this.showItemForm = false;
    this.editItem = null;
  }

  saveItem() {
    if (this.itemForm.invalid) { this.itemForm.markAllAsTouched(); return; }
    this.savingItem = true;
    const payload = this.itemForm.value;

    const obs = this.editItem
      ? this.api.updateInventoryItem(this.id, this.editItem.id, payload)
      : this.api.addInventoryItem(this.id, payload);

    obs.subscribe({
      next: () => {
        this.toast.success(this.editItem ? 'Item updated.' : 'Item added.');
        this.closeItemForm();
        this.loadInventory();
      },
      error: () => this.toast.error('Failed to save item.'),
      complete: () => setTimeout(() => this.savingItem = false)
    });
  }

  deleteItem(item: InventoryItem) {
    if (!confirm(`Remove "${item.name}" from inventory?`)) return;
    this.api.deleteInventoryItem(this.id, item.id).subscribe({
      next: () => { this.toast.success('Item removed.'); this.loadInventory(); },
      error: () => this.toast.error('Failed to remove item.')
    });
  }

  conditionClass(c: string) {
    return c === 'Good' ? 'badge-green' : c === 'Fair' ? 'badge-yellow' : 'badge-red';
  }

  hasError(field: string) {
    const ctrl = this.itemForm.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
