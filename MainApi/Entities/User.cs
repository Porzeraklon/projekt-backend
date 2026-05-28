using System.ComponentModel.DataAnnotations;
using SharedModels.Enums;

namespace MainApi.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }

    public string? ContactInfo { get; set; }

    // Relacja 1 do wielu (Jeden użytkownik może mieć wiele zgłoszeń)
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}