namespace eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

public class UsersMicroservicePolicies : IUsersMicroservicePolicies
{
    private readonly ILogger<UsersMicroservicePolicies> _logger;

    public UsersMicroservicePolicies(ILogger<UsersMicroservicePolicies> logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// This retry policy will attempt to retry the HTTP request to the Users microservice up to 5 times with an exponential backoff strategy (waiting 2^n seconds between retries). It will only retry for transient failures, which are defined as HTTP responses that do not have a successful status code (i.e., not in the 200-299 range). The onRetry callback is used to log each retry attempt, including the attempt number and the wait time before the next retry. This helps in monitoring and diagnosing issues with the Users microservice.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// This circuit breaker policy will monitor the HTTP requests to the Users microservice and will open the circuit if there are 3 consecutive failures (either exceptions or specific HTTP status codes). When the circuit is open, any further requests will fail immediately for a duration of 2 minutes. After the break duration, the circuit will enter a half-open state where it will allow a limited number of test requests to determine if the Users microservice has recovered. The onBreak, onReset, and onHalfOpen callbacks are used to log the state changes of the circuit breaker, which is crucial for monitoring the health of the Users microservice and understanding when it is experiencing issues.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// This timeout policy will ensure that if the Users microservice does not respond within 5 seconds, the request will be cancelled and a timeout exception will be thrown. This prevents the Orders microservice from waiting indefinitely for a response and allows it to handle the timeout scenario gracefully.
    /// </summary>
    /// <returns></returns>
    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        AsyncTimeoutPolicy<HttpResponseMessage> timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(5),
            onTimeoutAsync: (context, timespan, task) =>
            {
                _logger.LogWarning($"Timeout after {timespan.TotalSeconds} seconds.");
                return Task.CompletedTask;
            });

        return timeoutPolicy;
    }
}