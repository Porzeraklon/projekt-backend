using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MainApi.Services;




public class RabbitModelPooledObjectPolicy : IPooledObjectPolicy<IModel>
{
    private readonly IConnection _connection;

    public RabbitModelPooledObjectPolicy(IConnection connection)
    {
        _connection = connection;
    }

    public IModel Create()
    {

        return _connection.CreateModel();
    }

    public bool Return(IModel obj)
    {

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




public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly ObjectPool<IModel> _channelPool;

    public RabbitMqService(IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:HostName"] ?? "localhost";
        var factory = new ConnectionFactory() { HostName = host };


        _connection = factory.CreateConnection();


        var policy = new RabbitModelPooledObjectPolicy(_connection);
        var provider = new DefaultObjectPoolProvider
        {
            MaximumRetained = Environment.ProcessorCount * 2
        };
        _channelPool = provider.Create(policy);
    }

    public void PublishTicketCreatedEvent<T>(T message)
    {

        var channel = _channelPool.Get();

        try
        {

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

            _channelPool.Return(channel);
        }
    }

    public void Dispose()
    {

        _connection?.Close();
        _connection?.Dispose();
    }
}
