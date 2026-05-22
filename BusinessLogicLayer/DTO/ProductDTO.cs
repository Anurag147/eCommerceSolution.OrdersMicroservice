namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record ProductDTO(Guid ProductID,
    string ProductName,
    int Category,
    decimal UnitPrice,
    int QuantityInStock);