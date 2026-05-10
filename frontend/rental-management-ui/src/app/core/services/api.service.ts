import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';
import { Property, InventoryItem } from '../models/property.model';
import { Lease } from '../models/lease.model';
import { Payment } from '../models/payment.model';
import { MaintenanceRequest } from '../models/maintenance.model';
import { DashboardData } from '../models/dashboard.model';
import { ProvisionTemplate, LeaseProvision, LeaseProvisionPayload } from '../models/provision.model';

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

  getInviteEmail(token: string) {
    return this.http.get<{ email: string }>(`${this.base}/auth/invite/${token}`);
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

  getLeasePdf(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/leases/${id}/pdf`, { responseType: 'blob' });
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

  getPaymentReceipt(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/payments/${id}/receipt`, { responseType: 'blob' });
  }

  createStripeCheckout(leaseId: string) {
    return this.http.post<{ checkoutUrl: string }>(`${this.base}/leases/${leaseId}/payments/stripe-checkout`, {});
  }

  // --- Maintenance ---

  getMaintenanceRequests() {
    return this.http.get<MaintenanceRequest[]>(`${this.base}/maintenance`);
  }

  createMaintenanceRequest(payload: { propertyId: string; title: string; description: string; priority: string; imageUrl?: string }) {
    return this.http.post<MaintenanceRequest>(`${this.base}/maintenance`, payload);
  }

  resolveMaintenanceRequest(id: string, resolutionNotes?: string) {
    return this.http.patch<MaintenanceRequest>(`${this.base}/maintenance/${id}/resolve`, { resolutionNotes });
  }

  // --- Reminders ---

  getReminderSettings(leaseId: string) {
    return this.http.get<{ id: string; leaseId: string; isEnabled: boolean; daysBeforeDue: number; daysAfterDue: number }>(`${this.base}/reminders/${leaseId}`);
  }

  updateReminderSettings(leaseId: string, payload: { isEnabled: boolean; daysBeforeDue: number; daysAfterDue: number }) {
    return this.http.put<{ id: string; leaseId: string; isEnabled: boolean; daysBeforeDue: number; daysAfterDue: number }>(`${this.base}/reminders/${leaseId}`, payload);
  }

  // --- Dashboard ---

  getDashboard() {
    return this.http.get<DashboardData>(`${this.base}/dashboard`);
  }

  // --- Storage ---

  getUploadUrl(bucket: string, path: string) {
    const params = new HttpParams().set('bucket', bucket).set('path', path);
    return this.http.get<{ uploadUrl: string }>(`${this.base}/storage/upload-url`, { params });
  }

  // --- Provision Templates ---

  getProvisionTemplates() {
    return this.http.get<ProvisionTemplate[]>(`${this.base}/provision-templates`);
  }

  createProvisionTemplate(payload: { title: string; body: string }) {
    return this.http.post<ProvisionTemplate>(`${this.base}/provision-templates`, payload);
  }

  updateProvisionTemplate(id: string, payload: { title: string; body: string }) {
    return this.http.put<ProvisionTemplate>(`${this.base}/provision-templates/${id}`, payload);
  }

  deleteProvisionTemplate(id: string) {
    return this.http.delete<void>(`${this.base}/provision-templates/${id}`);
  }

  // --- Lease Provisions ---

  getLeaseProvisions(leaseId: string) {
    return this.http.get<LeaseProvision[]>(`${this.base}/leases/${leaseId}/provisions`);
  }

  setLeaseProvisions(leaseId: string, provisions: LeaseProvisionPayload[]) {
    return this.http.put<LeaseProvision[]>(`${this.base}/leases/${leaseId}/provisions`, provisions);
  }

  deleteLeaseProvision(leaseId: string, provisionId: string) {
    return this.http.delete<void>(`${this.base}/leases/${leaseId}/provisions/${provisionId}`);
  }
}
