using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalManagementApi.Data;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class PdfService(AppDbContext db)
{
    public async Task<byte[]> GenerateLeasePdfAsync(Guid leaseId, Guid requestorId, string requestorRole)
    {
        var lease = await db.Leases
            .Include(l => l.Property).ThenInclude(p => p.Landlord)
            .Include(l => l.Tenant)
            .Include(l => l.Provisions.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(l => l.Id == leaseId);

        if (lease is null) throw new KeyNotFoundException("Lease not found.");

        var hasAccess = requestorRole switch
        {
            "Landlord" => lease.Property.LandlordId == requestorId,
            "Tenant"   => lease.TenantId == requestorId,
            _          => false
        };

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Rental Property Manager")
                        .SemiBold().FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().Text("LEASE AGREEMENT")
                        .Bold().FontSize(22).FontColor(Colors.Black);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Text("Property").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", lease.Property.Name);
                        AddRow(t, "Address", lease.Property.Address);
                        AddRow(t, "Type", lease.Property.Type.ToString());
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Parties").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Landlord", $"{lease.Property.Landlord.FirstName} {lease.Property.Landlord.LastName}");
                        AddRow(t, "Tenant", $"{lease.Tenant.FirstName} {lease.Tenant.LastName}");
                        AddRow(t, "Tenant Email", lease.Tenant.Email);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Lease Terms").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Start Date", lease.StartDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "End Date", lease.EndDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "Monthly Rent", $"₱{lease.MonthlyRent:N2}");
                        AddRow(t, "Security Deposit", $"₱{lease.DepositAmount:N2}");
                        AddRow(t, "Advance Payment", $"₱{lease.AdvanceAmount:N2}");
                        AddRow(t, "Status", lease.Status.ToString());
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    if (lease.Provisions.Any())
                    {
                        col.Item().Text("Special Provisions").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);

                        foreach (var provision in lease.Provisions)
                        {
                            col.Item().Column(inner =>
                            {
                                inner.Item().PaddingBottom(2).Text(provision.Title).SemiBold();
                                inner.Item().Text(provision.Body).FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    }

                    col.Item().Text("Signatures").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(40).Text("Landlord Signature");
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text($"{lease.Property.Landlord.FirstName} {lease.Property.Landlord.LastName}")
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("Date: _______________").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(40).Text("Tenant Signature");
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text($"{lease.Tenant.FirstName} {lease.Tenant.LastName}")
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("Date: _______________").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("This document was generated electronically by Rental Property Manager on ");
                    text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy")).SemiBold();
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GeneratePaymentReceiptPdfAsync(Guid paymentId, Guid requestorId, string requestorRole)
    {
        var payment = await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property).ThenInclude(p => p.Landlord)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null) throw new KeyNotFoundException("Payment not found.");

        var hasAccess = requestorRole switch
        {
            "Landlord" => payment.Lease.Property.LandlordId == requestorId,
            "Tenant"   => payment.Lease.TenantId == requestorId,
            _          => false
        };

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        if (payment.Status == PaymentStatus.Unpaid)
            throw new InvalidOperationException("No receipt available for unpaid payments.");

        var receiptRef = payment.Id.ToString().Replace("-", "")[^8..].ToUpper();
        var balance = payment.AmountDue - payment.AmountPaid;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Rental Property Manager")
                        .SemiBold().FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().Text("PAYMENT RECEIPT")
                        .Bold().FontSize(22).FontColor(Colors.Black);
                    col.Item().Text($"Receipt #{receiptRef}")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Text("Property").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", payment.Lease.Property.Name);
                        AddRow(t, "Address", payment.Lease.Property.Address);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Tenant").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", $"{payment.Lease.Tenant.FirstName} {payment.Lease.Tenant.LastName}");
                        AddRow(t, "Email", payment.Lease.Tenant.Email);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Payment Details").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Due Date", payment.DueDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "Amount Due", $"₱{payment.AmountDue:N2}");
                        AddRow(t, "Amount Paid", $"₱{payment.AmountPaid:N2}");
                        AddRow(t, "Balance", $"₱{balance:N2}");
                        AddRow(t, "Status", payment.Status.ToString());
                        if (payment.PaidAt.HasValue)
                            AddRow(t, "Date Paid", payment.PaidAt.Value.ToString("MMMM dd, yyyy"));
                        if (!string.IsNullOrEmpty(payment.Notes))
                            AddRow(t, "Notes", payment.Notes);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by Rental Property Manager on ");
                    text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy")).SemiBold();
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor t, string label, string value)
    {
        t.Cell().PaddingVertical(4).Text(label).SemiBold().FontColor(Colors.Grey.Darken2);
        t.Cell().PaddingVertical(4).Text(value);
    }
}
