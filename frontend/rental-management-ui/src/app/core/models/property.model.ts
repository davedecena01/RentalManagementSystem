export interface Property {
  id: string;
  landlordId: string;
  name: string;
  address: string;
  type: 'Apartment' | 'House' | 'Condo' | 'Commercial';
  description?: string;
  isOccupied: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface InventoryItem {
  id: string;
  propertyId: string;
  name: string;
  description?: string;
  condition: 'Good' | 'Fair' | 'Poor';
  createdAt: string;
}
