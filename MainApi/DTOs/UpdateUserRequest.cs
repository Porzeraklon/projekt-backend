using System.ComponentModel.DataAnnotations;
using SharedModels.Enums;

namespace MainApi.DTOs;

public class UpdateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Role Role { get; set; }

    public string? ContactInfo { get; set; }

    // Opcjonalne - wypełniane tylko w przypadku zmiany/resetu hasła
    public string? Password { get; set; }
}