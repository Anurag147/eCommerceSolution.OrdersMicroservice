using eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using eCommerce.OrdersMicroService.API.Middleware;
using eCommerce.OrdersMicroService.BusinessLogicLayer;
using eCommerce.OrdersMicroService.DataAccessLayer;
using FluentValidation.AspNetCore;
using AutoMapper;
using eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using Polly;
using eCommerce.OrdersMicroService.BusinessLogicLayer.Policies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddBusinessLogicLayer(builder.Configuration);
builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(cfg => { },
    typeof(OrderAddRequestToOrderMappingProfile).Assembly
);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>();
builder.Services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();

// Register HttpClient for UsersMicroservice with Polly retry policy
builder.Services.AddHttpClient<UsersMicroserviceClient>(client => {
    client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
})
.AddPolicyHandler((sp, request) => sp.GetRequiredService<IUsersMicroservicePolicies>().GetRetryPolicy())//adding retry policy to users microservice http client
.AddPolicyHandler((sp, request) => sp.GetRequiredService<IUsersMicroservicePolicies>().GetCircuitBreakerPolicy())//adding circuit breaker policy to users microservice http client
.AddPolicyHandler((sp, request) => sp.GetRequiredService<IUsersMicroservicePolicies>().GetTimeoutPolicy());//adding timeout policy to users microservice http client

builder.Services.AddHttpClient<ProductsMicroserviceClient>(client => {
  client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
})
.AddPolicyHandler((sp, request) => sp.GetRequiredService<IProductsMicroservicePolicies>().GetFallbackPolicy())//adding fallback policy to products microservice http client return empty product details when products microservice is down or returns failure response
.AddPolicyHandler((sp, request) => sp.GetRequiredService<IProductsMicroservicePolicies>().GetBulkheadPolicy());//adding bulkhead policy to products microservice http client to limit concurrent requests and prevent cascading failures when products microservice is under heavy load

var app = builder.Build();
app.UseExceptionHandlingMiddleware();
app.UseCors();
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
