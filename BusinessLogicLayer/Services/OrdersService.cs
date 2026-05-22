using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;
using eCommerce.OrdersMicroService.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;

public class OrdersService : IOrdersService
{
    public readonly IOrdersRepository _ordersRepository;
    public readonly IMapper _mapper;
    public readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    public readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    public readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
    public readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    public readonly UsersMicroserviceClient _usersMicroserviceClient;
    public readonly ProductsMicroserviceClient _productsMicroserviceClient;
    public OrdersService(IOrdersRepository ordersRepository, IMapper mapper, IValidator<OrderAddRequest> orderAddRequestValidator, IValidator<OrderItemAddRequest> orderItemAddRequestValidator, IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator, IValidator<OrderUpdateRequest> orderUpdateRequestValidator, UsersMicroserviceClient usersMicroserviceClient, ProductsMicroserviceClient productsMicroserviceClient)
    {
        _ordersRepository = ordersRepository;
        _mapper = mapper;
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequestValidator;
        _usersMicroserviceClient = usersMicroserviceClient;
        _productsMicroserviceClient = productsMicroserviceClient;
    }

    /// <summary>
    /// Adds a new order to the system after validating the input request. It first checks if the orderAddRequest is null and throws an ArgumentNullException if it is. Then, it validates the orderAddRequest using the _orderAddRequestValidator. If the validation fails, it throws a ValidationException with the validation errors. Next, it iterates through each order item in the orderAddRequest and validates them using the _orderItemAddRequestValidator. If any of the order items fail validation, it throws a ValidationException with the respective errors. Finally, if all validations pass, it maps the orderAddRequest to an Order entity, adds it to the repository, and returns the mapped OrderResponse.
    /// </summary>
    /// <param name="orderAddRequest"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ValidationException"></exception>
    public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
    {
        if (orderAddRequest == null)
        {
            throw new ArgumentNullException(nameof(orderAddRequest));
        }
        var validationResult = await _orderAddRequestValidator.ValidateAsync(orderAddRequest);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        //Add logic to check if the user exists in the Users Microservice
        var user = await _usersMicroserviceClient.GetUserByIdAsync(orderAddRequest.UserID);
        if (user == null)
        {
            throw new ValidationException($"User with ID {orderAddRequest.UserID} does not exist.");
        }

       foreach (var orderItem in orderAddRequest.OrderItems)
{
    try
    {
        var orderItemValidationResult =
            await _orderItemAddRequestValidator.ValidateAsync(orderItem);

        // Check if the product exists in the Products Microservice
        var product =
            await _productsMicroserviceClient
                .GetProductByProductID(orderItem.ProductID);

                Console.WriteLine(
    $"Checked product with ID {orderItem.ProductID} in Products Microservice. " +
    $"{(product != null ? "Product exists." : "Product does not exist.")}");

if (product != null)
{
    Console.WriteLine("Product Details:");
    Console.WriteLine($"ProductID: {product.ProductID}");
    Console.WriteLine($"ProductName: {product.ProductName}");
    Console.WriteLine($"Category: {product.Category}");
    Console.WriteLine($"UnitPrice: {product.UnitPrice}");
    Console.WriteLine($"QuantityInStock: {product.QuantityInStock}");
}
        if (product == null)
        {
            throw new ValidationException(
                $"Product with ID {orderItem.ProductID} does not exist.");
        }

        if (!orderItemValidationResult.IsValid)
        {
            throw new ValidationException(
                orderItemValidationResult.Errors);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Exception occurred while processing order item");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");

        if (ex.InnerException != null)
        {
            Console.WriteLine(
                $"Inner Exception: {ex.InnerException.Message}");

            Console.WriteLine(
                $"Inner StackTrace: {ex.InnerException.StackTrace}");
        }
    }
}
       var orderInput = _mapper.Map<Order>(orderAddRequest);

decimal totalBill = 0;

foreach (var orderItem in orderInput.OrderItems)
{
    orderItem.TotalPrice =
        decimal.Round(
            orderItem.Quantity * orderItem.UnitPrice,
            2,
            MidpointRounding.AwayFromZero);

    totalBill += orderItem.TotalPrice;

    Console.WriteLine(
        $"OrderItem => ProductID: {orderItem.ProductID}, " +
        $"Quantity: {orderItem.Quantity}, " +
        $"UnitPrice: {orderItem.UnitPrice}, " +
        $"TotalPrice: {orderItem.TotalPrice}");
}

orderInput.TotalBill =
    decimal.Round(
        totalBill,
        2,
        MidpointRounding.AwayFromZero);

Console.WriteLine($"Final TotalBill: {orderInput.TotalBill}");

        Order? addedOrder = await _ordersRepository.AddOrder(orderInput);
        if (addedOrder == null)
        {
            throw new Exception("Failed to add order.");
        }
        return _mapper.Map<OrderResponse>(addedOrder);
    }

    /// <summary>
    /// Deletes an existing order from the system based on the provided orderID. It first retrieves the order to be deleted using the GetOrderByCondition method of the repository. If the order is not found, it returns false. If the order is found, it proceeds to delete the order using the DeleteOrder method of the repository and returns the result of the deletion operation.
    /// </summary>
    /// <param name="orderID"></param>
    /// <returns></returns>
    public async Task<bool> DeleteOrder(Guid orderID)
    {
        Order? orderToDelete = await _ordersRepository.GetOrderByCondition(Builders<Order>.Filter.Eq(o => o.OrderID, orderID));
        if (orderToDelete == null)
        {
            return false;
        }
        return await _ordersRepository.DeleteOrder(orderID);
    }
    /// <summary>
    /// Retrieves an order from the system based on a specified filter condition. It uses the GetOrderByCondition method of the repository to fetch the order that matches the provided filter. If no order is found, it returns null. If an order is found, it maps the Order entity to an OrderResponse DTO and returns it.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        Order? order = await _ordersRepository.GetOrderByCondition(filter);
        if (order == null)
        {
            return null;
        }
        return _mapper.Map<OrderResponse>(order);
    }

    /// <summary>
    /// Retrieves a list of all orders from the system. It uses the GetAllOrders method of the repository to fetch all orders, maps the list of Order entities to a list of OrderResponse DTOs, and returns the list of OrderResponse objects.
    /// </summary>
    /// <returns></returns>

    public async Task<List<OrderResponse?>> GetOrders()
    {
        var orders = await _ordersRepository.GetAllOrders();
        List<OrderResponse?> orderResponses = _mapper.Map<List<OrderResponse?>>(orders);
        Console.WriteLine($"Fetched {orderResponses.Count} orders from the repository.");

        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);
                Console.WriteLine($"Fetched product details for ProductID: {orderItemResponse.ProductID} from Products Microservice. " +
                    $"{(productDTO != null ? "Product exists." : "Product does not exist.")}");
                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        return orderResponses.ToList();
    }
    /// <summary>
    /// Retrieves a list of orders from the system based on a specified filter condition. It uses the GetOrdersByCondition method of the repository to fetch the orders that match the provided filter, maps the list of Order entities to a list of OrderResponse DTOs, and returns the list of OrderResponse objects.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        var orders = await _ordersRepository.GetOrdersByCondition(filter);
        List<OrderResponse?> orderResponses = _mapper.Map<List<OrderResponse?>>(orders);
        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        return orderResponses.ToList();
    }

    /// <summary>
    /// Updates an existing order in the system after validating the input request. It first checks if the orderUpdateRequest is null and throws an ArgumentNullException if it is. Then, it validates the orderUpdateRequest using the _orderUpdateRequestValidator. If the validation fails, it throws a ValidationException with the validation errors. Next, it iterates through each order item in the orderUpdateRequest and validates them using the _orderItemUpdateRequestValidator. If any of the order items fail validation, it throws a ValidationException with the respective errors. Finally, if all validations pass, it maps the orderUpdateRequest to an Order entity, updates it in the repository, and returns the mapped OrderResponse.
    /// </summary>
    /// <param name="orderUpdateRequest"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ValidationException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
    {
        if (orderUpdateRequest == null)
        {
            throw new ArgumentNullException(nameof(orderUpdateRequest));
        }
        var validationResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        //Add logic to check if the user exists in the Users Microservice
        var user = await _usersMicroserviceClient.GetUserByIdAsync(orderUpdateRequest.UserID);
        if (user == null)
        {
            throw new ValidationException($"User with ID {orderUpdateRequest.UserID} does not exist.");
        }
        foreach (var orderItem in orderUpdateRequest.OrderItems)
        {
            var orderItemValidationResult = await _orderItemUpdateRequestValidator.ValidateAsync(orderItem);
            var product = await _productsMicroserviceClient.GetProductByProductID(orderItem.ProductID);
            if (product == null)
            {
                throw new ValidationException($"Product with ID {orderItem.ProductID} does not exist.");
            }
            if (!orderItemValidationResult.IsValid)
            {
                throw new ValidationException(orderItemValidationResult.Errors);
            }
        }
        var orderInput = _mapper.Map<Order>(orderUpdateRequest);
        foreach (var orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }
        orderInput.TotalBill = orderInput.OrderItems.Sum(oi => oi.TotalPrice);

        Order? updatedOrder = await _ordersRepository.UpdateOrder(orderInput);
        if (updatedOrder == null)
        {
            throw new Exception("Failed to update order.");
        }
        return _mapper.Map<OrderResponse>(updatedOrder);
    }
}