using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroService.DataAccessLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.API.APIControllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _ordersService;
    public OrdersController(IOrdersService ordersService)
    {
        _ordersService = ordersService;
    }

    //Get: api/Orders
    [HttpGet]
    public async Task<IEnumerable<OrderResponse?>> GetOrders()
    {
        Console.WriteLine("GetOrders endpoint hit");

        var result = await _ordersService.GetOrders();

        Console.WriteLine($"Result Count: {result.Count()}");

        return result;
    }

    //Get: api/Orders/search/orderid/{orderID}
    [HttpGet("search/orderid/{orderID}")]
    public async Task<ActionResult<OrderResponse?>> GetOrderByID(Guid orderID)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.OrderID, orderID);
        var order = await _ordersService.GetOrderByCondition(filter);
        if (order == null)
        {
            return NotFound();
        }
        return order;
    }

    //GET: /api/Orders/search/userid/{userID}
    [HttpGet("search/userid/{userID}")]
    public async Task<IEnumerable<OrderResponse?>> GetOrdersByUserID(Guid userID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.UserID, userID);
        List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);
        return orders;
    }

    //Get: api/Orders/search/productid/{productID}
    [HttpGet("search/productid/{productID}")]
    public async Task<ActionResult<OrderResponse?>> GetOrderByProductID(Guid productID)
    {
        var filter = Builders<Order>.Filter.ElemMatch(o => o.OrderItems, oi => oi.ProductID == productID);
        var order = await _ordersService.GetOrderByCondition(filter);
        if (order == null)
        {
            return NotFound();
        }
        return order;
    }

    //Get: api/Orders/search/orderDate/{orderDate}
    [HttpGet("search/orderDate/{orderDate}")]
    public async Task<ActionResult<List<OrderResponse?>>> GetOrdersByOrderDate(DateTime orderDate)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.OrderDate.ToString("yyyy-MM-dd"), orderDate.ToString("yyyy-MM-dd"));
        var orders = await _ordersService.GetOrdersByCondition(filter);
        if (orders == null || orders.Count == 0)
        {
            return NotFound();
        }
        return orders;
    }

    //Post: api/Orders
    [HttpPost]
    public async Task<ActionResult<OrderResponse?>> CreateOrder([FromBody] OrderAddRequest orderRequest)
    {
        var order = await _ordersService.AddOrder(orderRequest);
        if (order == null)
        {
            return BadRequest();
        }
        return CreatedAtAction(nameof(GetOrderByID), new { orderID = order.OrderID }, order);
    }

    //PUT: api/Orders/{orderID}}
    [HttpPut("{orderID}")]
    public async Task<ActionResult<OrderResponse?>> UpdateOrder(
   Guid orderID,
   [FromBody] OrderUpdateRequest orderRequest)
    {
        if (orderID != orderRequest.OrderID)
        {
            return BadRequest();
        }
        var order = await _ordersService.UpdateOrder(orderRequest);
        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }

    //DELETE api/Orders/{orderID}
    [HttpDelete("{orderID}")]
    public async Task<IActionResult> Delete(Guid orderID)
    {
        if (orderID == Guid.Empty)
        {
            return BadRequest("Invalid order ID");
        }

        bool isDeleted = await _ordersService.DeleteOrder(orderID);

        if (!isDeleted)
        {
            return Problem("Error in adding product");
        }

        return Ok(isDeleted);
    }
}