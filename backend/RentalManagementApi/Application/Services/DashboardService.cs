using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Dashboard;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardDto> GetLandlordDashboardAsync(Guid landlordId)
    {
        var propertyIds = await db.Properties
            .Where(p => p.LandlordId == landlordId)
            .Select(p => p.Id)
            .ToListAsync();

        var totalProperties = propertyIds.Count;

        var activeLeases = await db.Leases
            .Where(l => propertyIds.Contains(l.PropertyId) && l.Status == LeaseStatus.Active)
            .ToListAsync();

        var occupiedIds = activeLeases.Select(l => l.PropertyId).ToHashSet();
        var occupied = occupiedIds.Count;
        var vacant = totalProperties - occupied;
        var totalMonthlyRent = activeLeases.Sum(l => l.MonthlyRent);

        var leaseIds = await db.Leases
            .Where(l => propertyIds.Contains(l.PropertyId))
            .Select(l => l.Id)
            .ToListAsync();

        var allPayments = await db.Payments
            .Where(p => leaseIds.Contains(p.LeaseId))
            .ToListAsync();

        var totalUnpaid = allPayments
            .Where(p => p.Status == PaymentStatus.Unpaid || p.Status == PaymentStatus.Partial)
            .Sum(p => p.AmountDue - p.AmountPaid);

        var openMaintenance = await db.MaintenanceRequests
            .Where(m => propertyIds.Contains(m.PropertyId) && m.Status != MaintenanceStatus.Resolved)
            .CountAsync();

        var monthlyIncome = allPayments
            .Where(p => p.PaidAt.HasValue)
            .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyIncomePoint
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Sum(p => p.AmountPaid)
            })
            .ToList();

        var breakdown = new PaymentBreakdown
        {
            Paid = allPayments.Count(p => p.Status == PaymentStatus.Paid),
            Partial = allPayments.Count(p => p.Status == PaymentStatus.Partial),
            Unpaid = allPayments.Count(p => p.Status == PaymentStatus.Unpaid)
        };

        return new DashboardDto
        {
            TotalProperties = totalProperties,
            OccupiedProperties = occupied,
            VacantProperties = vacant,
            TotalMonthlyRent = totalMonthlyRent,
            TotalUnpaidRent = totalUnpaid,
            OpenMaintenanceCount = openMaintenance,
            MonthlyIncome = monthlyIncome,
            PaymentBreakdown = breakdown
        };
    }

    public async Task<DashboardDto> GetTenantDashboardAsync(Guid tenantId)
    {
        var activeLease = await db.Leases
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Status == LeaseStatus.Active);

        if (activeLease is null) return new DashboardDto();

        var payments = await db.Payments
            .Where(p => p.LeaseId == activeLease.Id)
            .ToListAsync();

        var totalUnpaid = payments
            .Where(p => p.Status == PaymentStatus.Unpaid || p.Status == PaymentStatus.Partial)
            .Sum(p => p.AmountDue - p.AmountPaid);

        var monthlyIncome = payments
            .Where(p => p.PaidAt.HasValue)
            .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyIncomePoint
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Sum(p => p.AmountPaid)
            })
            .ToList();

        var breakdown = new PaymentBreakdown
        {
            Paid = payments.Count(p => p.Status == PaymentStatus.Paid),
            Partial = payments.Count(p => p.Status == PaymentStatus.Partial),
            Unpaid = payments.Count(p => p.Status == PaymentStatus.Unpaid)
        };

        return new DashboardDto
        {
            TotalMonthlyRent = activeLease.MonthlyRent,
            TotalUnpaidRent = totalUnpaid,
            PaymentBreakdown = breakdown,
            MonthlyIncome = monthlyIncome
        };
    }
}
