namespace eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroService.BusinessLogicLayer.DTO;
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
        var response = await _httpClient.GetAsync($"/api/users/{userId}");
        if (response.IsSuccessStatusCode)
        {
            if(response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }
            else if(response.StatusCode == System.Net.HttpStatusCode.BadRequest)
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
}