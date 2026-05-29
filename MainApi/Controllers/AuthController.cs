using MainApi.Data;
using MainApi.DTOs;
using MainApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Nieprawidłowy email lub hasło." });
        }

        // Zgodnie ze specyfikacją: Admin ma bezwzględny obowiązek korzystania z 2FA
        if (user.Role == SharedModels.Enums.Role.Admin)
        {
            if (user.TwoFactorEnabled)
            {
                // Admin ma już 2FA, prosimy o kod weryfikacyjny
                return Ok(new { 
                    requires2FA = true, 
                    message = "Wymagana weryfikacja 2FA. Przejdź do endpointu /api/auth/verify-2fa" 
                });
            }
            else
            {
                // Wymuszenie konfiguracji 2FA dla nowego Admina
                // Przesyłamy Secret do frontendu, by Vue mogło wygenerować kod QR
                return Ok(new {
                    requires2FASetup = true,
                    message = "Wymagana konfiguracja 2FA. Zeskanuj kod QR w aplikacji Authenticator.",
                    secretKey = user.TwoFactorSecret
                });
            }
        }

        // Dla zwykłych pracowników od razu generujemy token
        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new { token });
    }

    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return BadRequest(new { message = "Użytkownik nie istnieje lub nie ma skonfigurowanego 2FA." });
        }

        // Weryfikacja kodu z Google Authenticator / Authy za pomocą Otp.NET
        var base32Bytes = Base32Encoding.ToBytes(user.TwoFactorSecret);

        var totp = new Totp(base32Bytes);

        // Weryfikacja kodu z oknem czasowym (tzw. Time Tolerance) na wypadek asynchronizacji zegarów
        bool isValid = totp.VerifyTotp(request.Code, out long timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);

        if (!isValid)
        {
            return Unauthorized(new { message = "Nieprawidłowy lub przeterminowany kod 2FA." });
        }

        // ====================================================================
        // DODANO: Zapis aktywacji 2FA do bazy po pierwszym pomyślnym podaniu kodu
        // ====================================================================
        if (!user.TwoFactorEnabled)
        {
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
        }

        // Kod jest poprawny, wydajemy pełnoprawny token JWT
        var token = _tokenService.GenerateJwtToken(user);

        return Ok(new { token });
    }
}