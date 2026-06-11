using Microsoft.Extensions.Hosting;

namespace BusinessLogicLayer.RabbitMQ
{
    public class RabbitMQProductDeleteHostedService : IHostedService
    {
        //Add consumer here and call consume method in start async
        private readonly IRabbitMQProductDeleteConsumer _consumer;
        public RabbitMQProductDeleteHostedService(IRabbitMQProductDeleteConsumer consumer)
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