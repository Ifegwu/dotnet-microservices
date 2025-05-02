using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Catalog.Service.Entities;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Common.Identity;
using Play.Catalog.Service;

var builder = WebApplication.CreateBuilder(args);

// Bind ServiceSettings from appsettings.json
// builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection("ServiceSettings"));

var serviceSettings = builder.Configuration.GetSection("ServiceSettings").Get<ServiceSettings>();

// Register MongoDB services
// builder.Services.AddMongo(builder.Configuration);

// Register MongoDB repository for Item entity
builder.Services.AddMongo()
                .AddMongoRepository<Item>("items")
                .AddMassTransitWithRabbitMq()
                .AddJwtBearerAuthentication();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Read, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.readaccess", "catalog.fullaccess");
    });
    options.AddPolicy(Policies.Write, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.writeaccess", "catalog.fullaccess");
    });
});
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//                 .AddJwtBearer(options =>
//                 {
//                     options.Authority = "https://localhost:5003";
//                     options.Audience = serviceSettings.ServiceName;
//                     options.RequireHttpsMetadata = false;

//                     options.Events = new JwtBearerEvents
//                     {
//                         OnAuthenticationFailed = context =>
//                         {
//                             Console.WriteLine($"Token validation failed: {context.Exception.Message}");
//                             return Task.CompletedTask;
//                         },
//                         OnTokenValidated = context =>
//                         {
//                             Console.WriteLine("Token successfully validated.");
//                             return Task.CompletedTask;
//                         }
//                     };
//                 });

// Add services to the container.
builder.Services.AddControllers();
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

// app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
