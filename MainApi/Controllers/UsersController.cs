using MainApi.Data;
using MainApi.DTOs;
using MainApi.Entities;
using SharedModels.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(Role.Admin))] // Blokada całego kontrolera - tylko dla ról Admin
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET: /api/users (Pobieranie listy wszystkich użytkowników)
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.TwoFactorEnabled,
                u.ContactInfo
            })
            .ToListAsync();

        return Ok(users);
    }

    // =========================================================
    // GET: /api/users/{id} (Pobieranie jednego użytkownika)
    // =========================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.TwoFactorEnabled,
                u.ContactInfo
            })
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "Nie znaleziono użytkownika." });
        }

        return Ok(user);
    }

    // =========================================================
    // POST: /api/users (Tworzenie nowego użytkownika przez Admina)
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Sprawdzamy czy email nie jest już zajęty
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return BadRequest(new { message = "Użytkownik o podanym adresie email już istnieje." });
        }

        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            ContactInfo = request.ContactInfo,
            TwoFactorEnabled = false // Nowy użytkownik musi najpierw przejść setup przy logowaniu
        };

        // Zgodnie ze specyfikacją: Jeśli tworzymy nowego Admina, generujemy dla niego sekret TOTP
        if (request.Role == Role.Admin)
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            newUser.TwoFactorSecret = Base32Encoding.ToString(key);
        }

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, new
        {
            message = "Użytkownik został pomyślnie utworzony.",
            userId = newUser.Id,
            requires2FASetup = newUser.Role == Role.Admin // Informacja dla frontendu
        });
    }

    // =========================================================
    // PUT: /api/users/{id} (Aktualizacja danych użytkownika)
    // =========================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "Nie znaleziono użytkownika." });
        }

        // Sprawdzamy unikalność emaila, jeśli został zmieniony
        if (user.Email != request.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id);
            if (emailExists)
            {
                return BadRequest(new { message = "Podany adres email jest już zajęty przez innego użytkownika." });
            }
        }

        // Aktualizacja pól podstawowych
        user.Email = request.Email;
        user.ContactInfo = request.ContactInfo;

        // Jeśli zmieniła się rola na Admina i nie miał wcześniej sekretu 2FA
        if (request.Role == Role.Admin && user.Role != Role.Admin && string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            user.TwoFactorSecret = Base32Encoding.ToString(key);
            user.TwoFactorEnabled = false; // Wymuszenie ponownej konfiguracji
        }
        
        user.Role = request.Role;

        // Opcjonalna zmiana hasła (jeśli admin przekazał nową wartość w DTO)
        if (!string.IsNullOrEmpty(request.Password))
        {
            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Nowe hasło musi mieć co najmniej 6 znaków." });
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Dane użytkownika zostały zaktualizowane." });
    }

    // =========================================================
    // DELETE: /api/users/{id} (Usuwanie użytkownika)
    // =========================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(new { message = "Nie znaleziono użytkownika." });
        }

        // Zabezpieczenie: Admin nie może usunąć samego siebie
        var currentUserId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value 
                         ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                         
        if (currentUserId == id.ToString())
        {
            return BadRequest(new { message = "Nie możesz usunąć własnego konta administratora." });
        }
        
        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { message = "Użytkownik został usunięty z systemu." });
        }
        catch (DbUpdateException)
        {
            // Łapiemy wyjątek z bazy danych (np. naruszenie klucza obcego przez przypisane tickety)
            return Conflict(new { 
                message = "Nie można usunąć tego użytkownika, ponieważ posiada on przypisane zgłoszenia (tickety) w systemie." 
            });
        }
        catch (Exception)
        {
            // Fallback dla innych, nieprzewidzianych błędów
            return StatusCode(500, new { message = "Wystąpił nieoczekiwany błąd podczas usuwania użytkownika." });
        }
    }
}