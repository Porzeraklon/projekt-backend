using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MainApi.Services;

public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqService(IConfiguration configuration)
    {
        // Czytamy hosta z konfiguracji, by Docker mógł to łatwo nadpisać
        var host = configuration["RabbitMQ:HostName"] ?? "localhost";
        var factory = new ConnectionFactory() { HostName = host };
        
        // Ustanawiamy JEDNO globalne połączenie dla całego cyklu życia API
        _connection = factory.CreateConnection();
    }

    public void PublishTicketCreatedEvent<T>(T message)
    {
        // Kanały (Channels) są lekkie - otwieramy je per publikacja (nie są thread-safe)
        using var channel = _connection.CreateModel();

        channel.QueueDeclare(queue: "ticket_notifications",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "",
                             routingKey: "ticket_notifications",
                             basicProperties: null,
                             body: body);
    }

    public void Dispose()
    {
        // Zamykamy połączenie, gdy aplikacja (API) jest wyłączana
        _connection?.Close();
        _connection?.Dispose();
    }
}