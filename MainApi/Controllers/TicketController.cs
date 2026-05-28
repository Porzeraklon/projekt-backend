using MainApi.Data;
using MainApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedModels.Entities;
using SharedModels.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Zabezpiecza CAŁY kontroler - wymaga tokenu JWT
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: /api/tickets (Dostępne TYLKO dla Administratora)
    // =========================================================
    [HttpGet]
    [Authorize(Roles = nameof(Role.Admin))]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Creator) // Dołączamy relację (dane pracownika)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Category,
                t.CreatedAt,
                // Zgodnie ze specyfikacją - admin musi widzieć kontakt do twórcy
                CreatorEmail = t.Creator!.Email,
                CreatorContactInfo = t.Creator.ContactInfo
            })
            .OrderByDescending(t => t.CreatedAt) // Najnowsze na górze
            .ToListAsync();

        return Ok(tickets);
    }

    // =========================================================
    // POST: /api/tickets (Dostępne dla zalogowanych)
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        // 1. Bezpieczne wyciągnięcie ID użytkownika z tokenu JWT
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            return Unauthorized(new { message = "Nie można zidentyfikować użytkownika na podstawie tokenu." });
        }

        // 2. Utworzenie encji i zapis w bazie
        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            CreatorId = userId // Przypisanie autora
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // 3. TODO: Komunikacja Asynchroniczna
        // W kolejnym etapie dodamy tutaj kod, który wyśle TicketCreatedEvent 
        // na kolejkę RabbitMQ, żeby Worker Service to odebrał i powiadomił front!

        return Ok(new { 
            message = "Zgłoszenie zostało pomyślnie utworzone.", 
            ticketId = ticket.Id 
        });
    }
}