using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Validation;
using SbomQualityGate.Worker;
using SbomQualityGate.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

//
// ------------------------------
// Configuration
// ------------------------------
//

var connectionString = builder.Configuration.GetConnectionString("Default");

//
// ------------------------------
// Database
// ------------------------------
//

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

//
// ------------------------------
// Infrastructure (Persistence)
// ------------------------------
//

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

builder.Services.AddScoped<ProcessNextValidationJobHandler>();

//
// ------------------------------
// Worker Services (Long-lived)
// ------------------------------
//

builder.Services.AddSingleton<JobProcessor>();

builder.Services.AddSingleton<PostgresNotificationListener>(sp => new PostgresNotificationListener(connectionString!));

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
host.Run();
