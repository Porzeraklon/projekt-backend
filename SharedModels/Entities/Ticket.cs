using System.ComponentModel.DataAnnotations;
using SharedModels.Enums;

namespace SharedModels.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketCategory Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Klucz obcy i właściwość nawigacyjna do twórcy zgłoszenia
    public Guid CreatorId { get; set; }
    public User? Creator { get; set; }
    // Dodaj tę linijkę w klasie Ticket:
    public bool IsArchived { get; set; } = false;
}