using eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using eCommerce.OrdersMicroService.API.Middleware;
using eCommerce.OrdersMicroService.BusinessLogicLayer;
using eCommerce.OrdersMicroService.DataAccessLayer;
using FluentValidation.AspNetCore;
using AutoMapper;
using eCommerce.OrdersMicroService.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

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

builder.Services.AddHttpClient<UsersMicroserviceClient>(client => {
  client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
});


builder.Services.AddHttpClient<ProductsMicroserviceClient>(client => {
  client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
}); 

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
