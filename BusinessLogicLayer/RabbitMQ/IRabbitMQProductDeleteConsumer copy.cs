namespace BusinessLogicLayer.RabbitMQ
{
    public interface IRabbitMQProductDeleteConsumer
    {
        Task Consume();
    }
}