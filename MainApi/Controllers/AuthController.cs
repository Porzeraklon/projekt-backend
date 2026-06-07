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

        if (user.Role == SharedModels.Enums.Role.Admin)
        {
            // Hasło się zgadza. Generujemy token Pre-Auth!
            var preAuthToken = _tokenService.GeneratePreAuthToken(user);

            if (user.TwoFactorEnabled)
            {
                return Ok(new { 
                    requires2FA = true, 
                    message = "Wymagana weryfikacja 2FA. Przejdź do endpointu /api/auth/verify-2fa",
                    preAuthToken = preAuthToken // Zwracamy go do frontendu
                });
            }
            else
            {
                return Ok(new {
                    requires2FASetup = true,
                    message = "Wymagana konfiguracja 2FA. Zeskanuj kod QR w aplikacji Authenticator.",
                    secretKey = user.TwoFactorSecret,
                    preAuthToken = preAuthToken // Zwracamy go do frontendu
                });
            }
        }

        // Dla zwykłych pracowników od razu wydajemy token
        var token = _tokenService.GenerateJwtToken(user);
        return Ok(new { token });
    }

    [HttpPost("verify-2fa")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        // 1. Odkodowujemy PreAuth Token by udowodnić, że krok logowania został wykonany
        var userId = _tokenService.ValidatePreAuthTokenAndGetUserId(request.PreAuthToken);
        
        if (userId == null)
        {
            return Unauthorized(new { message = "Nieprawidłowy lub wygasły token pre-autoryzacji. Zaloguj się ponownie." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return BadRequest(new { message = "Użytkownik nie istnieje lub nie ma skonfigurowanego 2FA." });
        }

        // 2. Weryfikacja kodu z Google Authenticator
        var base32Bytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
        var totp = new Totp(base32Bytes);

        bool isValid = totp.VerifyTotp(request.Code, out long timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);
        
        if (!isValid)
        {
            return Unauthorized(new { message = "Nieprawidłowy lub przeterminowany kod 2FA." });
        }

        // 3. Kod poprawny i tożsamość potwierdzona! Wydajemy pełnoprawny token.
        if (!user.TwoFactorEnabled)
        {
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
        }

        var token = _tokenService.GenerateJwtToken(user);
        return Ok(new { token });
    }
}
