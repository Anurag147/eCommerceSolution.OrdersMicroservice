using DnsClient.Internal;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
  private readonly HttpClient _httpClient;
  private readonly IDistributedCache _cache;
  private readonly ILogger<ProductsMicroserviceClient> _logger;

  public ProductsMicroserviceClient(HttpClient httpClient, IDistributedCache cache, ILogger<ProductsMicroserviceClient> logger)
  {
    _httpClient = httpClient;
    _cache = cache;
    _logger = logger;
  }

  public async Task<ProductDTO?> GetProductByProductID(Guid productID)
  {
    string cacheKey = $"product:{productID}";

    _logger.LogInformation("Checking cache for key {CacheKey}", cacheKey);

    string? cachedProduct = await _cache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedProduct))
    {
      _logger.LogInformation("Cache HIT for key {CacheKey}", cacheKey);

      return System.Text.Json.JsonSerializer.Deserialize<ProductDTO>(cachedProduct);
    }

    _logger.LogInformation("Cache MISS for key {CacheKey}", cacheKey);

    HttpResponseMessage response =
        await _httpClient.GetAsync($"/api/products/search/product-id/{productID}");

    _logger.LogInformation(
        "Product API returned status code {StatusCode} for ProductId {ProductId}",
        response.StatusCode,
        productID);

    if (!response.IsSuccessStatusCode)
    {
      if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        _logger.LogWarning("Product {ProductId} not found", productID);
        return null;
      }

      if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
      {
        _logger.LogWarning("Bad request for ProductId {ProductId}", productID);
        throw new HttpRequestException(
            "Bad request",
            null,
            System.Net.HttpStatusCode.BadRequest);
      }

      throw new HttpRequestException(
          $"Http request failed with status code {response.StatusCode}");
    }

    ProductDTO? product =
        await response.Content.ReadFromJsonAsync<ProductDTO>();

    if (product == null)
    {
      _logger.LogError(
          "Product API returned success but deserialized product was null for ProductId {ProductId}",
          productID);

      throw new ArgumentException("Invalid Product ID");
    }

    try
    {
      _logger.LogInformation(
          "Writing product to Redis with key {CacheKey}",
          cacheKey);

      await _cache.SetStringAsync(
          cacheKey,
          System.Text.Json.JsonSerializer.Serialize(product),
          new DistributedCacheEntryOptions
          {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5)
          });

      _logger.LogInformation(
          "Redis write completed for key {CacheKey}",
          cacheKey);

      var verify = await _cache.GetStringAsync(cacheKey);

      _logger.LogInformation(
          "Redis verification for key {CacheKey}: Found={Found}",
          cacheKey,
          !string.IsNullOrEmpty(verify));
    }
    catch (Exception ex)
    {
      _logger.LogError(
          ex,
          "Redis write failed for key {CacheKey}",
          cacheKey);

      throw;
    }

    return product;
  }
}