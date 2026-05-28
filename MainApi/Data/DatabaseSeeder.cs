using SharedModels.Entities;
using SharedModels.Enums;
using OtpNet;

namespace MainApi.Data;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext context)
    {
        // Tworzy bazę danych (jeśli nie istnieje) na podstawie Twojego DbContextu
        context.Database.EnsureCreated(); 

        if (!context.Users.Any())
        {
            // Generowanie klucza 2FA dla Admina
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);

            var admin = new User
            {
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = Role.Admin,
                TwoFactorEnabled = true,
                TwoFactorSecret = base32Secret,
                ContactInfo = "Główny Serwer"
            };

            var employee = new User
            {
                Email = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Role = Role.Employee,
                TwoFactorEnabled = false,
                ContactInfo = "Biurko 42"
            };

            context.Users.AddRange(admin, employee);
            context.SaveChanges();

            // Wypisujemy tajny klucz jawnym tekstem do testów
            Console.WriteLine("\n==================================================");
            Console.WriteLine(" DATA SEEDING: Utworzono konta testowe!");
            Console.WriteLine($" Admin Email: {admin.Email}");
            Console.WriteLine($" Admin Hasło: Admin123!");
            Console.WriteLine($" Admin 2FA Secret: {base32Secret}");
            Console.WriteLine("==================================================\n");
        }
    }
}