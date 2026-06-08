using System.ComponentModel.DataAnnotations;

namespace MainApi.DTOs;

public class VerifyTwoFactorRequest
{

    [Required]
    public string PreAuthToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}
