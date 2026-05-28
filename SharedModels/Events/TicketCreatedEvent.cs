namespace SharedModels.Events;

public class TicketCreatedEvent
{
    public Guid TicketId { get; set; } // <-- ZMIANA Z int NA Guid
    public string Title { get; set; } = string.Empty;
    public string CreatorEmail { get; set; } = string.Empty;
}