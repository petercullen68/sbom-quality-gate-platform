using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Process;
using SbomQualityGate.Infrastructure.Seed;
using SbomQualityGate.Infrastructure.Telemetry;
using SbomQualityGate.Infrastructure.Validation;
using SbomQualityGate.Worker;
using SbomQualityGate.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

//
// ------------------------------
// Configuration
// ------------------------------
//

var connectionString = builder.Configuration.GetConnectionString("Default");


//
// ------------------------------
// Logging - Serilog
// ------------------------------
//
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();


builder.Services.AddSerilog();

//
// ------------------------------
// Database
// ------------------------------
//

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));


// ------------------------------
// // Telemetry - OpenTelemetry
// // ------------------------------


builder.AddSbomQualityGateTelemetry();

//
// ------------------------------
// Infrastructure (Persistence)
// ------------------------------
//

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();
builder.Services.AddScoped<IValidationResultRepository, ValidationResultRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// ------------------------------
// Infrastructure (External Tools)
// ------------------------------
//
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<SbomQsCircuitBreaker>();   // ← singleton: shared state across scopes
builder.Services.AddScoped<IValidationTool, SbomQsValidationTool>();

//
// ------------------------------
// Application (Use Cases)
// ------------------------------
//

builder.Services.AddScoped<ProcessNextValidationJobHandler>();

//
// ------------------------------
// Worker Services (Long-lived)
// ------------------------------
//

builder.Services.AddSingleton<JobProcessor>();

builder.Services.AddSingleton<PostgresNotificationListener>(_ => new PostgresNotificationListener(connectionString!));

// ------------------------------
// Seed Data
// ------------------------------
//
builder.Services.AddHostedService<SeedDataService>();

//
// ------------------------------
// Hosted Worker
// ------------------------------
//

builder.Services.AddHostedService<Worker>();

//
// ------------------------------
// Build & Run
// ------------------------------
//

var host = builder.Build();
try
{
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
