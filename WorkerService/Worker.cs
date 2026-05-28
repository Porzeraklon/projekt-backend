using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedModels.Events;
using WorkerService.Hubs;

namespace WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHubContext<NotificationHub> _hubContext; // <-- Dodajemy SignalR
    private IConnection _connection = null!;
    private IModel _channel = null!;

    // Wstrzykujemy HubContext do przesyłania wiadomości przez WebSockety
    public Worker(ILogger<Worker> logger, IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        InitRabbitMQ();
    }

    private void InitRabbitMQ()
    {
        // Kluczowe: ustawiamy DispatchConsumersAsync = true dla asynchronicznego SignalR
        var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "ticket_notifications",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
                             
        _logger.LogInformation("Podłączono do RabbitMQ. Nasłuchiwanie kolejki 'ticket_notifications'...");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        // Używamy Asynchronicznego konsumenta, żeby prawidłowo await-ować WebSockety
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var ticketEvent = JsonSerializer.Deserialize<TicketCreatedEvent>(message);

            _logger.LogInformation($"[RABBITMQ -> WEBSOCKET] Przesyłam powiadomienie o tickecie {ticketEvent?.TicketId}...");

            // MAGIA WEBSOCKETÓW: Wysyłamy powiadomienie do wszystkich podłączonych klientów
            if (ticketEvent != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNewTicket", ticketEvent);
            }

            // Potwierdzamy Królikowi usunięcie wiadomości
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: "ticket_notifications",
                             autoAck: false, 
                             consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}