using System.ComponentModel.DataAnnotations;

namespace RentalManagementApi.DTOs.Auth;

public class AcceptInviteRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string SupabaseUserId { get; set; } = string.Empty;
}
