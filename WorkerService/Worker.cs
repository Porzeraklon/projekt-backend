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
        
        // Logika asynchronicznego odbierania wiadomości
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // 1. Bezpieczna deserializacja
                var ticketEvent = JsonSerializer.Deserialize<TicketCreatedEvent>(message);

                _logger.LogInformation($"Odebrano nowy ticket: {ticketEvent?.TicketId}. Przesyłam do Adminów...");
                
                if (ticketEvent != null)
                {
                    // 2. Próba wysyłki przez SignalR (też może rzucić wyjątkiem przy zerwanym sieci)
                    await _hubContext.Clients.Group("AdminsGroup").SendAsync("ReceiveNewTicket", ticketEvent);
                }

                // 3. Pełen sukces - potwierdzamy RabbitMQ, że wiadomość została przetworzona (usunięcie z kolejki)
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Łapiemy błędy deserializacji JSON, błędy SignalR i inne nieprzewidziane wyjątki
                _logger.LogError(ex, "Krytyczny błąd podczas przetwarzania wiadomości z RabbitMQ. Wiadomość zostanie odrzucona.");
                
                // Odrzucamy wiadomość BEZ ponownego wrzucania na kolejkę (requeue: false),
                // żeby uniknąć nieskończonej pętli przetwarzania tzw. "Poison Message".
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: "ticket_notifications", autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        // Dobrą praktyką jest również jawne wywołanie Dispose() dla kanałów i połączeń
        _channel?.Close();
        _channel?.Dispose();
        
        _connection?.Close();
        _connection?.Dispose();
        
        base.Dispose();
    }
}