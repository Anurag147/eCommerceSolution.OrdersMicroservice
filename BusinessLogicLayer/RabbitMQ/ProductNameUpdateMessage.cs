namespace BusinessLogicLayer.RabbitMQ
{
   public record ProductNameUpdateMessage(Guid productId, string? productName);
}