using System.ComponentModel.DataAnnotations;
using SharedModels.Enums;

namespace MainApi.DTOs;

public class CreateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Role Role { get; set; }

    public string? ContactInfo { get; set; }
}