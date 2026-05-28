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
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConfiguration _configuration;
    
    private IConnection _connection = null!;
    private IModel _channel = null!;

    public Worker(ILogger<Worker> logger, IHubContext<NotificationHub> hubContext, IConfiguration configuration)
    {
        _logger = logger;
        _hubContext = hubContext;
        _configuration = configuration;
        InitRabbitMQ();
    }

    private void InitRabbitMQ()
    {
        var rabbitHost = _configuration["RabbitMQ:HostName"] ?? "localhost";
        
        var factory = new ConnectionFactory { HostName = rabbitHost, DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "ticket_notifications",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
                             
        _logger.LogInformation($"[Worker] RabbitMQ Host: {rabbitHost} | Oczekuję na wiadomości...");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var ticketEvent = JsonSerializer.Deserialize<TicketCreatedEvent>(message);

            _logger.LogInformation($"Odebrano nowy ticket: {ticketEvent?.TicketId}. Przesyłam do Adminów...");
            
            if (ticketEvent != null)
            {
                await _hubContext.Clients.Group("AdminsGroup").SendAsync("ReceiveNewTicket", ticketEvent);
            }

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(queue: "ticket_notifications", autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}