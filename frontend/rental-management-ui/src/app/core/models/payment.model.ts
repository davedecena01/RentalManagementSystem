export interface Payment {
  id: string;
  leaseId: string;
  propertyName: string;
  tenantName: string;
  dueDate: string;
  amountDue: number;
  amountPaid: number;
  status: 'Unpaid' | 'Partial' | 'Paid';
  paidAt?: string;
  proofFileUrl?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}
