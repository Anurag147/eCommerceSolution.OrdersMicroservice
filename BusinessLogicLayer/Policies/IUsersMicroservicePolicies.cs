namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;
using eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using Polly;

public interface IUsersMicroservicePolicies
{
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
    IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy();
}