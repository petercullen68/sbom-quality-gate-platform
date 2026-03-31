using Microsoft.AspNetCore.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SbomQualityGate.Api.Configuration;
using SbomQualityGate.Api.Logging;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Seed;
using SbomQualityGate.Infrastructure.Telemetry;
using SbomQualityGate.Infrastructure.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Bind and validate upload settings once at startup.
builder.Services
    .AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .Validate(options => options.MaxUploadBytes > 0, "Upload:MaxUploadBytes must be greater than zero.")
    .ValidateOnStart();

var uploadOptions = builder.Configuration
    .GetSection(UploadOptions.SectionName)
    .Get<UploadOptions>() ?? new UploadOptions();

//
// ------------------------------
// Configuration
// ------------------------------
//
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' is missing or empty.");
}


//
// ------------------------------
// Logging
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

builder.Host.UseSerilog();

// ------------------------------
// Telemetry - OpenTelemetry
// ------------------------------
 
// API Program.cs — common + AspNetCore instrumentation
builder.AddSbomQualityGateTelemetry();
 
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation());
//
// ------------------------------
// // Global upload/request body limits
// ------------------------------
//
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadOptions.MaxUploadBytes;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadOptions.MaxUploadBytes;
});

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

//
// ------------------------------
// Infrastructure (Persistence)
// ------------------------------
//

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISbomFeatureRepository, SbomFeatureRepository>();
builder.Services.AddScoped<ISbomProfileRepository, SbomProfileRepository>();
builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();


//
// ------------------------------
// Application (Use Cases)
// ------------------------------
//

builder.Services.AddScoped<ISubmitSbomHandler, SubmitSbomHandler>();
builder.Services.AddSingleton<IReportDiscoveryTool, SbomQsReportDiscoveryTool>();
builder.Services.AddScoped<DiscoverSbomReportHandler>();

// ------------------------------
// Seed Data
// ------------------------------
//
builder.Services.AddHostedService<SeedDataService>();

//
// ------------------------------
// API Layer
// ------------------------------
//

builder.Services.AddControllers();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SBOM Quality Gate API", Version = "v1" });
});

var app = builder.Build();

//
// ------------------------------
// Middleware Pipeline
// ------------------------------
//

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
        ApiLog.UnhandledException(
            logger,
            context.Request.Method,
            context.Request.Path.ToString(),
            ex);
        
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = app.Environment.IsDevelopment() ? ex?.Message : null,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier
            }
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    });
});


app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

try
{
    app.Run();
}
catch (HostAbortedException)
{
    throw; // important for WebApplicationFactory/test host lifecycle
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
