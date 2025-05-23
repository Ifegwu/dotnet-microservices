using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Exceptions;
using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer;
using Play.Common.MassTransit;
using MassTransit;
using MassTransit.RetryPolicies;

var builder = WebApplication.CreateBuilder(args);
// Register Razor Pages
builder.Services.AddRazorPages();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigin = builder.Configuration["AllowedOrigin"];
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
var identityServerSettings = builder.Configuration.GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();

builder.Services.Configure<IdentitySettings>(builder.Configuration.GetSection(nameof(IdentitySettings)))
        .AddDefaultIdentity<ApplicationUser>()
        .AddRoles<ApplicationRole>()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
        (
            mongoDbSettings.ConnectionString,
            serviceSettings.ServiceName
        )
        .AddDefaultUI()
        .AddDefaultTokenProviders();

builder.Services.AddMassTransitWithRabbitMq(retryConfigurator =>
{
    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
    retryConfigurator.Ignore(typeof(UnknownUserException));
    retryConfigurator.Ignore(typeof(InsufficientFundsException));
});

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseSuccessEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseErrorEvents = true;
    options.EmitStaticAudienceClaim = true;
})
        .AddAspNetIdentity<ApplicationUser>()
        .AddInMemoryApiScopes(new[]
        {
            new ApiScope("catalog.fullaccess", "Full access to Catalog API"),
            new ApiScope("inventory.fullaccess", "Full access to Inventory API"),
            new ApiScope("trading.fullaccess", "Full access to Trading API"),
            new ApiScope("IdentityServerApi", "Identity Server API")
        })
        .AddInMemoryApiResources(identityServerSettings.ApiResources)
        .AddInMemoryClients(identityServerSettings.Clients)
        .AddInMemoryIdentityResources(new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource("roles", new[] { "role" })
        })
        .AddDeveloperSigningCredential();

builder.Services.AddLocalApiAuthentication();

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddHostedService<IdentitySeedHostedService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

app.UseStaticFiles();

app.UseRouting();

// Use CORS before authentication and authorization
app.UseCors();

app.UseIdentityServer();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

// Add error handling middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Run();