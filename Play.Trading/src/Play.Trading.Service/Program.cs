using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Common;
using Play.Common.Settings;
using Play.Common.MongoDB;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Trading.Service.StateMachines;
using Play.Trading.Service.SignalR;
using Microsoft.AspNetCore.SignalR;
using MassTransit;
using MassTransit.MongoDbIntegration;
using System;
using System.Reflection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.Settings;
using System.Text.Json;
using Play.Inventory.Contracts;
using Play.Identity.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMongo()
    .AddMongoRepository<CatalogItem>("catalogitems")
    .AddMongoRepository<InventoryItem>("inventoryitems")
    .AddMongoRepository<ApplicationUser>("users")
    .AddJwtBearerAuthentication();

AddMassTransit(builder);

void AddMassTransit(WebApplicationBuilder builder)
{
    builder.Services.AddMassTransit(configure =>
    {
        configure.AddConsumers(Assembly.GetEntryAssembly());
        configure.UsingPlayEconomyRabbitMQ(retryConfigurator =>
        {
            retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
            retryConfigurator.Ignore(typeof(UnknownItemException));
        });
        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaConfigurator =>
        {
            sagaConfigurator.UseInMemoryOutbox();
        })
        .MongoDbRepository(r =>
        {
            var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings))
                                                        .Get<ServiceSettings>();
            var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings))
                                                        .Get<MongoDbSettings>();

            if (serviceSettings == null || mongoSettings == null)
            {
                throw new InvalidOperationException("ServiceSettings or MongoDbSettings is not configured properly.");
            }

            r.Connection = mongoSettings.ConnectionString;
            r.DatabaseName = serviceSettings.ServiceName;
        });
    });

    var queueSettings = builder.Configuration.GetSection(nameof(QueueSettings)).Get<QueueSettings>();
    if (queueSettings == null)
        throw new InvalidOperationException("QueueSettings is not configured properly.");

    EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
    EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
    EndpointConvention.Map<SubtractItems>(new Uri(queueSettings.SubtractItemsQueueAddress));
}

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    // options.JsonSerializerOptions.IgnoreNullValues = true;
});

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Trading.Service", Version = "v1" });
});

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>()
       .AddSingleton<MessageHub>()
       .AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Trading.Service v1"));

    var allowedOrigin = app.Configuration["AllowedOrigin"];
    if (!string.IsNullOrEmpty(allowedOrigin))
    {
        app.UseCors(builder =>
        {
            builder.WithOrigins(allowedOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); //Credentials must be allowed in order for cookie-based sticky sessions to work correctly.
        });
    }
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<MessageHub>("/messagehub");
});

app.MapControllers();

app.Run();