using Microsoft.Extensions.Hosting;

namespace BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductUpdateHostedService : IHostedService
    {
        //Add consumer here and call consume method in start async
        private readonly IRabbitMQProductNameUpdateConsumer _consumer;
        public RabbitMQProductUpdateHostedService(IRabbitMQProductNameUpdateConsumer consumer)
        {
            _consumer = consumer;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
             _consumer.Consume();
             return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}