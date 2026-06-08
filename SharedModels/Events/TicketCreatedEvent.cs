namespace SharedModels.Events;

public class TicketCreatedEvent
{
    public Guid TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CreatorEmail { get; set; } = string.Empty;
}
