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
using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer;

var builder = WebApplication.CreateBuilder(args);
// Register Razor Pages
builder.Services.AddRazorPages();

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

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseSuccessEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseErrorEvents = true;
    options.EmitStaticAudienceClaim = true;
})
        .AddAspNetIdentity<ApplicationUser>()
        .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
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

app.UseIdentityServer();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
