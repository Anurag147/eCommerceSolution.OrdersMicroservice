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


}
