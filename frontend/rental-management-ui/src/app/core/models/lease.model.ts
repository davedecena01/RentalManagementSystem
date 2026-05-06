export interface Lease {
  id: string;
  propertyId: string;
  propertyName: string;
  propertyAddress: string;
  tenantId: string;
  tenantName: string;
  tenantEmail: string;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  depositAmount: number;
  advanceAmount: number;
  status: 'Active' | 'Terminated' | 'Expired';
  tenantIdFileUrl?: string;
  createdAt: string;
  updatedAt: string;
}
