using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using SQB.Auth.Application.Extensions;
using SQB.Auth.Domain.Models;
using SQB.Auth.Infrastructure;
using SQB.Auth.Infrastructure.Extensions;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {EventId}{NewLine}{Exception}")
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 10_000_000, // 10 MB
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {EventId} {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
builder.Services.RegisterServices();
builder.Services.RegisterRepositories();
builder.Services.Configure<IdentityStoreOptions>(options =>
{
    options.ConnectionString = configuration.GetConnectionString("Identity");
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("SQB Auth API")
            .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });
}

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
