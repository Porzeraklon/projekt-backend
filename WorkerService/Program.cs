using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkerService;
using WorkerService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja JWT
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

    // SignalR w przeglądarkach wysyła token w QueryStringu
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

builder.Services.AddSignalR();
builder.Services.AddHostedService<Worker>();

// Docelowo w środowisku prod zablokuj to tylko dla URL swojego frontendu!
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.SetIsOriginAllowed((host) => true) 
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials(); // SignalR tego wymaga
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Pamiętaj o middleware do autoryzacji PRZED MapHub
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notifications");

app.Run();