using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MainApi.Services;

public class RabbitMqService
{
    // Lokalnie łączymy się po localhost. W Dockerze będzie to nazwa kontenera.
    private readonly string _hostname = "localhost"; 

    public void PublishTicketCreatedEvent<T>(T message)
    {
        var factory = new ConnectionFactory() { HostName = _hostname };
        
        // Nawiązujemy połączenie z RabbitMQ
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Deklarujemy kolejkę (jeśli nie istnieje, to ją utworzy)
        channel.QueueDeclare(queue: "ticket_notifications",
                             durable: true,      // Kolejka przetrwa restart królika
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        // Zmieniamy obiekt w JSON-a i potem w tablicę bajtów
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // Wysyłamy wiadomość do kolejki
        channel.BasicPublish(exchange: "",
                             routingKey: "ticket_notifications",
                             basicProperties: null,
                             body: body);
    }
}