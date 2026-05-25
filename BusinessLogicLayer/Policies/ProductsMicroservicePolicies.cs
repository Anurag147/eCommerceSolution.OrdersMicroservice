namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

using System.Text;
using System.Text.Json;
using DnsClient.Internal;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;

public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly ILogger<ProductsMicroservicePolicies> _logger;

    public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// This fallback policy will be triggered when the HTTP request to the Products microservice fails (i.e., returns a non-success status code). When the fallback is triggered, it will log a warning message and return a dummy ProductDTO object with default values. This allows the Orders microservice to continue functioning even when the Products microservice is down or experiencing issues, providing a graceful degradation of service instead of a complete failure. The dummy product can be used by the Orders microservice to indicate that product details are temporarily unavailable, while still allowing order processing to continue.
    /// </summary>
    /// <returns></returns>
    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
      .FallbackAsync(async (context) =>
      {
          _logger.LogWarning("Fallback triggered: The request failed, returning dummy data");

          ProductDTO product = new ProductDTO(ProductID: Guid.Empty,
            ProductName: "Temporarily Unavailable (fallback)",
            Category: 0,
            UnitPrice: 0,
            QuantityInStock: 0
            );

          var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
          {
              Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
          };

          return response;
      });

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy()
    {
         return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: 3, // Maximum number of concurrent executions
            maxQueuingActions: 40, // Maximum number of actions that can be queued when the bulkhead is full
            onBulkheadRejectedAsync: context =>
            {                _logger.LogWarning("Bulkhead rejected execution: Too many concurrent requests to Products microservice.");
                return Task.CompletedTask;
            });
    }
}
