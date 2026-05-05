export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: 'Landlord' | 'Tenant';
  onboardingCompleted: boolean;
  createdAt: string;
}
