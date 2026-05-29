using System.ComponentModel.DataAnnotations;

namespace MainApi.DTOs;

public class VerifyTwoFactorRequest
{
    // Frontend prześle tu token pre-autoryzacyjny zwrócony przy logowaniu
    [Required]
    public string PreAuthToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}