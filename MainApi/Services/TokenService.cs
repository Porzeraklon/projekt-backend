using Microsoft.IdentityModel.Tokens;
using MainApi.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MainApi.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ==========================================================
    // 1. Zwykły token (Pełny dostęp)
    // ==========================================================
    public string GenerateJwtToken(User user)
    {
        return GenerateToken(
            user, 
            double.Parse(_configuration["Jwt:ExpireMinutes"]!), 
            _configuration["Jwt:Audience"]!
        );
    }

    // ==========================================================
    // 2. Token Pre-Autoryzacyjny (Tylko dla 2FA)
    // ==========================================================
    public string GeneratePreAuthToken(User user)
    {
        // Token ważny tylko 5 minut z unikalnym Audience, 
        // by nie działał na standardowych endpointach.
        return GenerateToken(user, 5, "TicketingPreAuth");
    }

    // ==========================================================
    // 3. Wewnętrzny generator
    // ==========================================================
    private string GenerateToken(User user, double expireMinutes, string audience)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: audience, // Tu ustalamy, czy to token pełny, czy pre-auth
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ==========================================================
    // 4. Manualna walidacja tokenu Pre-Auth
    // ==========================================================
    public Guid? ValidatePreAuthTokenAndGetUserId(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var jwtSettings = _configuration.GetSection("Jwt");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        try 
        {
            // Walidujemy token ręcznie, upewniając się, że to token PreAuth
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = "TicketingPreAuth", // Odmowa, jeśli użyto pełnego tokenu!
                ClockSkew = TimeSpan.Zero // Wymuszamy ścisłe 5 minut bez opóźnień
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdString = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            return null;
        }
        catch
        {
            // Łapie błędy wygaśnięcia, zepsutego podpisu lub złego Audience
            return null;
        }
    }
}