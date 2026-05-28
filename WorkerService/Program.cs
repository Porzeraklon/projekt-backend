using WorkerService;
using WorkerService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Rejestrujemy SignalR oraz naszego Workera dizałającego w tle
builder.Services.AddSignalR();
builder.Services.AddHostedService<Worker>();

// Wyłączamy CORS dla testów, żeby frontend mógł się swobodnie podłączyć
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .SetIsOriginAllowed((host) => true)
               .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Podpinamy Huba pod endpoint. Frontend będzie łączył się z ws://localhost:PORT/notifications
app.MapHub<NotificationHub>("/notifications");

app.Run();