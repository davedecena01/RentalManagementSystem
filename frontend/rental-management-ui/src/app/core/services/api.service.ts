import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';
import { Property, InventoryItem } from '../models/property.model';
import { Lease } from '../models/lease.model';
import { Payment } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // --- Users ---

  getMe() {
    return this.http.get<User>(`${this.base}/users/me`);
  }

  updateMe(payload: Partial<User>) {
    return this.http.patch<User>(`${this.base}/users/me`, payload);
  }

  // --- Auth ---

  register(payload: { supabaseUserId: string; firstName: string; lastName: string; email: string }) {
    return this.http.post<{ id: string; role: string }>(`${this.base}/auth/register`, payload);
  }

  inviteTenant(email: string) {
    return this.http.post<{ message: string; token: string; expiresAt: string }>(
      `${this.base}/auth/invite-tenant`, { email }
    );
  }

  acceptInvite(payload: { token: string; firstName: string; lastName: string; supabaseUserId: string }) {
    return this.http.post<{ id: string; role: string }>(`${this.base}/auth/accept-invite`, payload);
  }

  // --- Properties ---

  getProperties() {
    return this.http.get<Property[]>(`${this.base}/properties`);
  }

  getProperty(id: string) {
    return this.http.get<Property>(`${this.base}/properties/${id}`);
  }

  createProperty(payload: { name: string; address: string; type: string; description?: string }) {
    return this.http.post<Property>(`${this.base}/properties`, payload);
  }

  updateProperty(id: string, payload: { name?: string; address?: string; type?: string; description?: string }) {
    return this.http.put<Property>(`${this.base}/properties/${id}`, payload);
  }

  deleteProperty(id: string) {
    return this.http.delete<void>(`${this.base}/properties/${id}`);
  }

  // --- Property Inventory ---

  getInventory(propertyId: string) {
    return this.http.get<InventoryItem[]>(`${this.base}/properties/${propertyId}/inventory`);
  }

  addInventoryItem(propertyId: string, payload: { name: string; description?: string; condition: string }) {
    return this.http.post<InventoryItem>(`${this.base}/properties/${propertyId}/inventory`, payload);
  }

  updateInventoryItem(propertyId: string, itemId: string, payload: { name?: string; description?: string; condition?: string }) {
    return this.http.put<InventoryItem>(`${this.base}/properties/${propertyId}/inventory/${itemId}`, payload);
  }

  deleteInventoryItem(propertyId: string, itemId: string) {
    return this.http.delete<void>(`${this.base}/properties/${propertyId}/inventory/${itemId}`);
  }

  // --- Leases ---

  getLeases() {
    return this.http.get<Lease[]>(`${this.base}/leases`);
  }

  getLease(id: string) {
    return this.http.get<Lease>(`${this.base}/leases/${id}`);
  }

  createLease(payload: {
    propertyId: string;
    tenantEmail: string;
    startDate: string;
    endDate: string;
    monthlyRent: number;
    depositAmount: number;
    advanceAmount: number;
    tenantIdFileUrl?: string;
  }) {
    return this.http.post<Lease>(`${this.base}/leases`, payload);
  }

  terminateLease(id: string) {
    return this.http.patch<Lease>(`${this.base}/leases/${id}/terminate`, {});
  }

  // --- Payments ---

  getPayments(status?: string) {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<Payment[]>(`${this.base}/payments`, { params });
  }

  getPayment(id: string) {
    return this.http.get<Payment>(`${this.base}/payments/${id}`);
  }

  manualPay(paymentId: string, payload: { amountPaid: number; proofFileUrl?: string; notes?: string }) {
    return this.http.post<Payment>(`${this.base}/payments/${paymentId}/manual-pay`, payload);
  }

  createStripeCheckout(leaseId: string) {
    return this.http.post<{ checkoutUrl: string }>(`${this.base}/leases/${leaseId}/payments/stripe-checkout`, {});
  }

  // --- Storage ---

  getUploadUrl(bucket: string, path: string) {
    const params = new HttpParams().set('bucket', bucket).set('path', path);
    return this.http.get<{ uploadUrl: string }>(`${this.base}/storage/upload-url`, { params });
  }
}
