using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Payments;
using RentalManagementApi.Options;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController(
    PaymentService paymentService,
    PdfService pdfService,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var payments = role == "Landlord"
            ? await paymentService.GetForLandlordAsync(userId.Value, status)
            : await paymentService.GetForTenantAsync(userId.Value);

        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var payment = await paymentService.GetByIdAsync(id, userId.Value, role);
        if (payment is null) return NotFound(new ApiError("Payment not found.", "PAYMENT_NOT_FOUND"));

        return Ok(payment);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var bytes = await pdfService.GeneratePaymentReceiptPdfAsync(id, userId.Value, role);
            Response.Headers.Append("Content-Disposition", "inline; filename=\"payment-receipt.pdf\"");
            return File(bytes, "application/pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiError("Payment not found.", "PAYMENT_NOT_FOUND"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/manual-pay")]
    public async Task<IActionResult> ManualPay(Guid id, [FromBody] ManualPayRequest request)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (payment, error) = await paymentService.ManualPayAsync(id, userId.Value, role, request);

        return error switch
        {
            "NOT_FOUND" => NotFound(new ApiError("Payment not found.", error)),
            "FORBIDDEN" => Forbid(),
            "INVALID_AMOUNT" => BadRequest(new ApiError("Amount paid must be greater than zero.", error)),
            not null => BadRequest(new ApiError("Could not process payment.", error)),
            _ => Ok(payment)
        };
    }

    [HttpPost("/api/leases/{leaseId:guid}/payments/stripe-checkout")]
    [Authorize(Policy = "TenantPolicy")]
    public async Task<IActionResult> CreateStripeCheckout(Guid leaseId)
    {
        var tenantId = GetCurrentUserId();
        if (tenantId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var frontendUrl = appOptions.Value.FrontendUrl.TrimEnd('/');
        var (url, error) = await paymentService.CreateStripeCheckoutAsync(leaseId, tenantId.Value, frontendUrl);

        return error switch
        {
            "LEASE_NOT_FOUND" => NotFound(new ApiError("Lease not found.", error)),
            "NO_UNPAID_PAYMENT" => BadRequest(new ApiError("No unpaid payments found for this lease.", error)),
            not null => BadRequest(new ApiError("Could not create checkout session.", error)),
            _ => Ok(new StripeCheckoutResponse { CheckoutUrl = url! })
        };
    }

    [HttpPost("stripe-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = stripeOptions.Value.WebhookSecret;

        try
        {
            await paymentService.HandleStripeWebhookAsync(payload, signature, webhookSecret);
            return Ok();
        }
        catch (Stripe.StripeException ex)
        {
            return BadRequest(new ApiError(ex.Message, "STRIPE_WEBHOOK_ERROR"));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("user_role")?.Value ?? string.Empty;
}
