using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductNameUpdateConsumer : IRabbitMQProductNameUpdateConsumer, IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly ILogger<RabbitMQProductNameUpdateConsumer> _logger;

        public RabbitMQProductNameUpdateConsumer(
            IConfiguration configuration,
            ILogger<RabbitMQProductNameUpdateConsumer> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ_HostName"]!,
                UserName = configuration["RabbitMQ_UserName"]!,
                Password = configuration["RabbitMQ_Password"]!,
                Port = int.Parse(configuration["RabbitMQ_Port"]!)
            };
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
            try
            {
                await InitializeAsync();

                string routingKey = "product.name.updated";
                string queueName = "orders.product.update.name.queue";

                if (_channel == null)
                {
                    _logger.LogError(
                        "RabbitMQ channel initialization failed.");
                    throw new InvalidOperationException(
                        "RabbitMQ channel is null.");
                }

                string exchangeName =
                    "products.exchange"; // _configuration["RabbitMQ_Products_Exchange"]!; --- IGNORE ---

                _logger.LogInformation(
                    "Declaring queue {QueueName}",
                    queueName);

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation(
                    "Binding queue {QueueName} to exchange {ExchangeName} with routing key {RoutingKey}",
                    queueName,
                    exchangeName,
                    routingKey);

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

                        _logger.LogInformation(
                            "RabbitMQ message received. DeliveryTag={DeliveryTag}",
                            ea.DeliveryTag);

                        var productNameUpdateMessage =
                            JsonSerializer.Deserialize<ProductNameUpdateMessage>(
                                message);

                        if (productNameUpdateMessage == null)
                        {
                            _logger.LogWarning(
                                "Unable to deserialize ProductNameUpdateMessage. Payload={Payload}",
                                message);

                            await _channel.BasicNackAsync(
                                ea.DeliveryTag,
                                false,
                                false);

                            return;
                        }

                        _logger.LogInformation(
                            "Product update received. ProductId={ProductId}, ProductName={ProductName}",
                            productNameUpdateMessage.productId,
                            productNameUpdateMessage.productName);

                        // TODO: Update database

                        await _channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false);

                        _logger.LogInformation(
                            "Message acknowledged. DeliveryTag={DeliveryTag}",
                            ea.DeliveryTag);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing RabbitMQ message. DeliveryTag={DeliveryTag}",
                            ea.DeliveryTag);

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

                _logger.LogInformation(
                    "RabbitMQ consumer is now listening on queue {QueueName}",
                    queueName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    "Failed to start RabbitMQ Product Name Update consumer");

                throw;
            }
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