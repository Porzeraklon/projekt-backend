using SharedModels.Enums;
using System.ComponentModel.DataAnnotations;

namespace MainApi.DTOs;

public class CreateTicketRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TicketCategory Category { get; set; }
}
