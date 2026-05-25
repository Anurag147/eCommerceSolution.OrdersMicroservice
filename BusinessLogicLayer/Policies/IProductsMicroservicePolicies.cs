using Polly;

namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

public interface IProductsMicroservicePolicies
{
    // Define any policies related to the Products microservice here
    IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy();
}