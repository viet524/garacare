using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.Auth;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
