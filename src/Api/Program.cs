using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SBOM Quality Gate API", Version = "v1" });
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ISbomRepository, SbomRepository>();
builder.Services.AddScoped<IValidationJobRepository, ValidationJobRepository>();
builder.Services.AddScoped<IValidationResultRepository, ValidationResultRepository>();

// Validation tool
builder.Services.AddScoped<IValidationTool, SbomQsValidationTool>();

// Handler
builder.Services.AddScoped<ISubmitSbomHandler, SubmitSbomHandler>();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
