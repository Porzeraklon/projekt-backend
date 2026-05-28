using MainApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. POBRANIE CONNECTION STRINGA Z APPSETTINGS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. REJESTRACJA DBCONTEXT W KONTENERZE DI (Dependency Injection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString, 
        ServerVersion.AutoDetect(connectionString)
    ));

// Dodanie kontrolerów (standard w Web API)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Konfiguracja potoku HTTP (Middleware)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();