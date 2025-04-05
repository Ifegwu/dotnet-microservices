using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Extensions.Http;

// Add Swagger to the API
using Microsoft.OpenApi.Models;
using Play.Inventory.Service.Entities;
using Play.Common;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection("ServiceSettings"));
// builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryitems");

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        if (cert?.Subject.Contains("localhost") ?? false)
            return true; // Trust localhost certificates
        return errors == System.Net.Security.SslPolicyErrors.None;
    };
    return handler;
})
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));

// builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("RabbitMQSettings"));

// builder.Services.AddScoped<IRepository<InventoryItem>, MongoRepository<InventoryItem>>();
// builder.Services.AddScoped(typeof(IRepository<InventoryItem>), typeof(MongoRepository<InventoryItem>));

// Random jitterer = new Random();
// builder.Services.AddHttpClient<CatalogClient>(client =>
// {
//     client.BaseAddress = new Uri("https://localhost:5001");
// })
// // Retry Policy
// .AddPolicyHandler(HttpPolicyExtensions
//     .HandleTransientHttpError()
//     .Or<TimeoutRejectedException>()
//     .WaitAndRetryAsync(
//         5,
//         retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
//                         + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
//         onRetry: (outcome, timespan, retryAttempt, context) =>
//         {
//             using var scope = builder.Services.BuildServiceProvider().CreateScope();
//             var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogClient>>();
//             logger?.LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
//         }
//     ))
// // Circuit Breaker Policy
// .AddPolicyHandler(HttpPolicyExtensions
//     .HandleTransientHttpError()
//     .Or<TimeoutRejectedException>()
//     .CircuitBreakerAsync(
//         3,
//         TimeSpan.FromSeconds(15),
//         onBreak: (outcome, timespan, context) =>
//         {
//             using var scope = builder.Services.BuildServiceProvider().CreateScope();
//             var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogClient>>();
//             logger?.LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
//         },
//         onReset: (context) =>
//         {
//             using var scope = builder.Services.BuildServiceProvider().CreateScope();
//             var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogClient>>();
//             logger?.LogWarning("Closing the circuit...");
//         }
//     ))
// // Timeout Policy
// .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1)));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "A simple API for demo",
        Contact = new OpenApiContact { Name = "Developer", Email = "danielagbanyim@hotmail.com" }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
