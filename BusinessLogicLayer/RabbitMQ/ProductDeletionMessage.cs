namespace BusinessLogicLayer.RabbitMQ
{
    public record ProductDeletionMessage(Guid productId, string? ProductName);
}