namespace RentalManagementApi.DTOs.Dashboard;

public class DashboardDto
{
    public int TotalProperties { get; set; }
    public int OccupiedProperties { get; set; }
    public int VacantProperties { get; set; }
    public decimal TotalMonthlyRent { get; set; }
    public decimal TotalUnpaidRent { get; set; }
    public int OpenMaintenanceCount { get; set; }
    public List<MonthlyIncomePoint> MonthlyIncome { get; set; } = [];
    public PaymentBreakdown PaymentBreakdown { get; set; } = new();
}

public class MonthlyIncomePoint
{
    public string Month { get; set; } = string.Empty; // "2025-01"
    public decimal Amount { get; set; }
}

public class PaymentBreakdown
{
    public int Paid { get; set; }
    public int Partial { get; set; }
    public int Unpaid { get; set; }
}
