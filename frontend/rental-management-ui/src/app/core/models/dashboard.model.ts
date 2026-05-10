export interface MonthlyIncomePoint {
  month: string;
  amount: number;
}

export interface PaymentBreakdown {
  paid: number;
  partial: number;
  unpaid: number;
}

export interface DashboardData {
  totalProperties: number;
  occupiedProperties: number;
  vacantProperties: number;
  totalMonthlyRent: number;
  totalUnpaidRent: number;
  openMaintenanceCount: number;
  monthlyIncome: MonthlyIncomePoint[];
  paymentBreakdown: PaymentBreakdown;
}
