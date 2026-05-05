using System.ComponentModel.DataAnnotations;

namespace RentalManagementApi.DTOs.Auth;

public class InviteTenantRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}
