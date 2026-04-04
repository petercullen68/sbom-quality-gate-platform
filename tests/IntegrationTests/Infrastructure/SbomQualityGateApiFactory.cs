using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Seed;

namespace SbomQualityGate.IntegrationTests.Infrastructure;

// ReSharper disable once ClassNeverInstantiated.Global 
public class SbomQualityGateApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public SbomQualityGateApiFactory()
    {
        // In CI the connection string is injected via environment variable.
        // Locally it comes from appsettings.Test.json (gitignore).
        if (Environment.GetEnvironmentVariable("ConnectionStrings__Default") is null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                    optional: false,
                    reloadOnChange: false)
                .Build();

            var connectionString = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "Integration test connection string 'Default' not found in appsettings.Test.json.");

            Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Suppress SeedDataService
            var seedDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(SeedDataService));

            if (seedDescriptor != null)
                services.Remove(seedDescriptor);

            // Replace real sbomqs tool with stub — sbomqs is not available in CI
            var validationToolDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IValidationTool));

            if (validationToolDescriptor != null)
                services.Remove(validationToolDescriptor);

            services.AddScoped<IValidationTool, StubValidationTool>();
            services.AddScoped<ProcessNextValidationJobHandler>();
            
            // Replace real spec conformance tool — avoids outbound HTTP to GitHub in CI
            var specConformanceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISpecConformanceTool));

            if (specConformanceDescriptor != null)
                services.Remove(specConformanceDescriptor);

            services.AddScoped<ISpecConformanceTool, StubSpecConformanceTool>();
        });

        builder.UseEnvironment("Test");
    }

    // IAsyncLifetime — runs once when the fixture is created
    // Migrations run here so the schema exists before any test runs
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Delete in FK dependency order — children before parents
        context.ValidationResults.RemoveRange(context.ValidationResults);
        await context.SaveChangesAsync();

        context.ValidationJobs.RemoveRange(context.ValidationJobs);
        await context.SaveChangesAsync();

        context.Sboms.RemoveRange(context.Sboms);
        await context.SaveChangesAsync();

        context.SbomFeatures.RemoveRange(context.SbomFeatures);
        context.SbomProfiles.RemoveRange(context.SbomProfiles);
        await context.SaveChangesAsync();

        context.TeamMembers.RemoveRange(context.TeamMembers);
        await context.SaveChangesAsync();

        context.Products.RemoveRange(context.Products);
        await context.SaveChangesAsync();

        context.Teams.RemoveRange(context.Teams);
        await context.SaveChangesAsync();

        await SeedDefaultDataAsync(context);
    }

    public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(context);
    }

    private static async Task SeedDefaultDataAsync(AppDbContext context)
    {
        if (await context.Teams.AnyAsync())
            return;

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Default Team",
            CreatedAt = DateTime.UtcNow
        };

        context.Teams.Add(team);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            Name = "Default Product",
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);

        await context.SaveChangesAsync();
    }
}
