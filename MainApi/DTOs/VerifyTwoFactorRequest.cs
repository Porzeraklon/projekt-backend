using System.ComponentModel.DataAnnotations;

namespace MainApi.DTOs;

public class VerifyTwoFactorRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}