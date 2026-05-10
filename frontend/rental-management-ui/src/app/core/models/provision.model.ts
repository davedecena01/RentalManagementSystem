export interface ProvisionTemplate {
  id: string;
  landlordId: string;
  title: string;
  body: string;
  createdAt: string;
}

export interface LeaseProvision {
  id: string;
  leaseId: string;
  title: string;
  body: string;
  sortOrder: number;
  createdAt: string;
}

export interface LeaseProvisionPayload {
  title: string;
  body: string;
  sortOrder: number;
}
