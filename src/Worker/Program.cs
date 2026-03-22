using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Validation;
using SbomQualityGate.Worker;
using SbomQualityGate.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<JobProcessor>();

builder.Services.AddSingleton<PostgresNotificationListener>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var conn = config.GetConnectionString("Default");

    return new PostgresNotificationListener(conn!);
});

// Repositories
builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();
builder.Services.AddScoped<IValidationResultRepository, ValidationResultRepository>();

// Use case
builder.Services.AddScoped<ProcessNextValidationJobHandler>();

builder.Services.AddScoped<IValidationTool, SbomQsValidationTool>();

// Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
