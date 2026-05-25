namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

public class UsersMicroservicePolicies : IUsersMicroservicePolicies
{
    private readonly ILogger<UsersMicroservicePolicies> _logger;

    public UsersMicroservicePolicies(ILogger<UsersMicroservicePolicies> logger)
    {
        _logger = logger;
    }
    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning($"Retrying... Attempt: {retryAttempt}, Waiting: {timespan.TotalSeconds} seconds");
                }
            );
        return retryPolicy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy =
            Policy<HttpResponseMessage>
                // Network / transport failures
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>() // usually timeout related
                // Infrastructure/server-side failures only
                .OrResult(response =>
                       response.StatusCode == HttpStatusCode.InternalServerError      // 500
                    || response.StatusCode == HttpStatusCode.BadGateway              // 502
                    || response.StatusCode == HttpStatusCode.ServiceUnavailable      // 503
                    || response.StatusCode == HttpStatusCode.GatewayTimeout          // 504
                )

                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,

                    // Stop calls for 2 minutes
                    durationOfBreak: TimeSpan.FromMinutes(2),

                    onBreak: (outcome, timespan) =>
                    {
                        string reason = outcome.Exception != null
                            ? outcome.Exception.Message
                            : outcome.Result.StatusCode.ToString();

                        _logger.LogWarning(
                            "Users microservice circuit breaker OPEN for {Duration} seconds. Reason: {Reason}",
                            timespan.TotalSeconds,
                            reason);
                    },

                    onReset: () =>
                    {
                        _logger.LogInformation(
                            "Users microservice circuit breaker RESET. Traffic resumed.");
                    },

                    onHalfOpen: () =>
                    {
                        _logger.LogInformation(
                            "Users microservice circuit breaker HALF-OPEN. Testing recovery.");
                    });

        return circuitBreakerPolicy;
    }
}