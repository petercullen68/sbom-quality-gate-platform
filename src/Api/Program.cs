using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Api.Configuration;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

//
// ------------------------------
// Configuration
// ------------------------------
//

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
// Request Limits / Upload Config
// ------------------------------
//

// Global upload/request body limits
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

var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

//
// ------------------------------
// Infrastructure (Persistence)
// ------------------------------
//

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISbomFeatureRepository, SbomFeatureRepository>();
builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();
builder.Services.AddScoped<IValidationResultRepository, ValidationResultRepository>();

//
// ------------------------------
// Infrastructure (External Tools)
// ------------------------------
//

builder.Services.AddScoped<IValidationTool, SbomQsValidationTool>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
