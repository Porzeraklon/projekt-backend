using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkerService;
using WorkerService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// KONFIGURACJA JWT
// ====================================================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };

    // SignalR w przeglądarkach wysyła token w QueryStringu (WebSockets nie obsługują nagłówków Authorization w przeglądarce)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ====================================================================
// REJESTRACJA SERWISÓW
// ====================================================================
builder.Services.AddSignalR();
builder.Services.AddHostedService<Worker>();

// --- KONFIGURACJA CORS ---
var allowedOriginsRaw = builder.Configuration["AllowedOrigins"];
var allowedOrigins = string.IsNullOrEmpty(allowedOriginsRaw) 
    ? Array.Empty<string>() 
    : allowedOriginsRaw.Split(';');

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // SignalR bezwzględnie tego wymaga!
        }
        else
        {
            // Fallback na środowisko lokalne
            policy.SetIsOriginAllowed((host) => true) 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); 
        }
    });
});

var app = builder.Build();

// ====================================================================
// KONFIGURACJA POTOKU HTTP (Middleware)
// ====================================================================
app.UseCors("FrontendPolicy"); // Używamy naszej nowej, bezpiecznej polityki

// Autoryzacja MUSI być przed MapHub!
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notifications");

app.Run();