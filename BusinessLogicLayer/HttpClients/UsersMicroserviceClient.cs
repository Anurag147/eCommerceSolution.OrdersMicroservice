namespace eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;

using eCommerce.OrdersMicroService.BusinessLogicLayer.DTO;
using Polly.CircuitBreaker;
using System.Net.Http.Json;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;

    public UsersMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserDTO?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new Exception("Bad request to Users Microservice");
                }
                else
                {
                    return await response.Content.ReadFromJsonAsync<UserDTO>();
                }
            }
            return null;
        }
        catch (BrokenCircuitException)
        {
            // Log the circuit breaker state (you can use a logging framework like Serilog, NLog, etc.)
            Console.WriteLine("Users Microservice is currently unavailable (circuit breaker is open).");
            throw; // Rethrow the exception to be handled by the calling code
        }
        catch (TimeoutException ex)
        {
            // Log the timeout exception (you can use a logging framework like Serilog, NLog, etc.)
            Console.WriteLine($"Request to Users Microservice timed out: {ex.Message}");
            throw; // Rethrow the exception to be handled by the calling code
        }
        catch (Exception ex)
        {
            // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
            Console.WriteLine($"Error calling Users Microservice: {ex.Message}");
            throw; // Rethrow the exception to be handled by the calling code
        }
    }
}