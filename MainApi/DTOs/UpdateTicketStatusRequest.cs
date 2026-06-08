using SharedModels.Enums;
using System.ComponentModel.DataAnnotations;

namespace MainApi.DTOs;

public class UpdateTicketStatusRequest
{
    [Required]
    public TicketStatus Status { get; set; }
}
