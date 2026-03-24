using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Api.Configuration;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//
// ------------------------------
// Configuration
// ------------------------------
//

var connectionString = builder.Configuration.GetConnectionString("Default");

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
builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();

//
// ------------------------------
// Application (Use Cases)
// ------------------------------
//

builder.Services.AddScoped<ISubmitSbomHandler, SubmitSbomHandler>();
builder.Services.AddScoped<DiscoverSbomFeaturesHandler>();

//
// ------------------------------
// API Layer
// ------------------------------
//

builder.Services.AddControllers();

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

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
