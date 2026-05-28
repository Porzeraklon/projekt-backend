using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MainApi.Services;

// ====================================================================
// 1. POLITYKA ZARZĄDZANIA KANAŁAMI (Jak pula ma tworzyć i niszczyć obiekty)
// ====================================================================
public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
{
    private readonly IConnection _connection;

    public RabbitModelPooledObjectPolicy(IConnection connection)
    {
        _connection = connection;
    }

    public IModel Create()
    {
        // Kiedy pula jest pusta, tworzy nowy kanał
        return _connection.CreateModel();
    }

    public bool Return(IModel obj)
    {
        // Kiedy zwracamy kanał do puli, sprawdzamy czy nadal żyje
        if (obj.IsOpen)
        {
            return true;
        }
        else
        {
            obj?.Dispose();
            return false;
        }
    }
}

// ====================================================================
// 2. GŁÓWNY SERWIS RABBITMQ
// ====================================================================
public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly ObjectPool<IModel> _channelPool;

    public RabbitMqService(IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:HostName"] ?? "localhost";
        var factory = new ConnectionFactory() { HostName = host };
        
        // Ustanawiamy JEDNO globalne połączenie dla całego cyklu życia API
        _connection = factory.CreateConnection();

        // Konfiguracja puli obiektów
        var policy = new RabbitModelPooledObjectPolicy(_connection);
        var provider = new DefaultObjectPoolProvider 
        { 
            MaximumRetained = Environment.ProcessorCount * 2 // Optymalna liczba trzymanych w pamięci kanałów
        };
        _channelPool = provider.Create(policy);
    }

    public void PublishTicketCreatedEvent<T>(T message)
    {
        // Pobieramy gotowy kanał z puli
        var channel = _channelPool.Get();
        
        try
        {
            // Deklaracja kolejki (idempotentna - jeśli istnieje, nic się nie stanie)
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
        finally
        {
            // ZAWSZE zwracamy kanał do puli, nawet jak poleci wyjątek!
            _channelPool.Return(channel);
        }
    }

    public void Dispose()
    {
        // Zamykamy połączenie przy wyłączaniu aplikacji
        _connection?.Close();
        _connection?.Dispose();
    }
}