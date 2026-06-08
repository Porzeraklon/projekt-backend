using MainApi.Data;
using MainApi.DTOs;
using MainApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MainApi.Entities;
using SharedModels.Enums;
using SharedModels.Events;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RabbitMqService _rabbitMqService;

    public TicketsController(AppDbContext context, RabbitMqService rabbitMqService)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
    }




    [HttpGet]
    [Authorize(Roles = nameof(Role.Admin))]
    public async Task<IActionResult> GetAllTickets([FromQuery] bool includeArchived = false)
    {
        var query = _context.Tickets
            .Include(t => t.Creator)
            .AsQueryable();


        if (!includeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        var tickets = await query
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Category,
                t.IsArchived,
                t.CreatedAt,
                CreatorEmail = t.Creator!.Email,
                CreatorContactInfo = t.Creator.ContactInfo
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tickets);
    }




    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Nie można zidentyfikować użytkownika." });
        }

        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            CreatorId = userId,
            IsArchived = false
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "nieznany@email.com";

        _rabbitMqService.PublishTicketCreatedEvent(new TicketCreatedEvent
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            CreatorEmail = userEmail
        });

        return Ok(new { message = "Zgłoszenie zostało utworzone.", ticketId = ticket.Id });
    }




    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(Role.Admin))]
    public async Task<IActionResult> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusRequest request)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound(new { message = "Nie znaleziono zgłoszenia." });
        }

        ticket.Status = request.Status;



        ticket.IsArchived = (request.Status == TicketStatus.Closed);

        await _context.SaveChangesAsync();

        return Ok(new {
            message = "Status zaktualizowany.",
            newStatus = ticket.Status,
            isArchived = ticket.IsArchived
        });
    }
}
