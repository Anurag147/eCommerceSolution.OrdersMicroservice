using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductDeleteConsumer : IRabbitMQProductDeleteConsumer, IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        public RabbitMQProductDeleteConsumer(IConfiguration configuration)
        {
            _configuration = configuration;
            // Initialize RabbitMQ connection and channel here

            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ_HostName"]!,
                UserName = configuration["RabbitMQ_UserName"]!,
                Password = configuration["RabbitMQ_Password"]!,
                Port = int.Parse(configuration["RabbitMQ_Port"]!)
            };

            InitializeAsync().GetAwaiter().GetResult();

        }
        public async Task InitializeAsync()
        {
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
            exchange: "products.exchange",
            type: ExchangeType.Direct,
            durable: true);
        }
        public async Task Consume()
        {
            string routingKey = "product.delete";
            string queueName = "orders.product.delete.queue";

            if (_channel == null)
                return;

            string exchangeName = "products.exchange";

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    string message =
                        Encoding.UTF8.GetString(ea.Body.ToArray());

                    var productDeletionMessage =
                        JsonSerializer.Deserialize<ProductDeletionMessage>(message);

                    if (productDeletionMessage != null)
                    {
                        // Update database here

                        Console.WriteLine(
                            $"Received product delete message for ProductId={productDeletionMessage.productId}");
                    }

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
                await _channel.DisposeAsync();

            if (_connection != null)
                await _connection.DisposeAsync();
        }
    }
}