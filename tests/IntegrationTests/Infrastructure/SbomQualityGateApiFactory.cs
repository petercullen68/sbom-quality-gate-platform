using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Infrastructure.Persistence;
using SbomQualityGate.Infrastructure.Seed;

namespace SbomQualityGate.IntegrationTests.Infrastructure;

public class SbomQualityGateApiFactory : WebApplicationFactory<Program>
{
    public SbomQualityGateApiFactory()
    {
        // Read test config before the API's WebApplication.CreateBuilder runs.
        // Setting as process environment variable ensures it is picked up by
        // the API's own config pipeline which we cannot intercept directly.
        var config = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                optional: false,
                reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Integration test connection string 'Default' not found in appsettings.Test.json.");

        Environment.SetEnvironmentVariable("ConnectionStrings__Default", connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Suppress SeedDataService — we seed explicitly in InitialiseDatabaseAsync
            // so we have full control over test data
            var descriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(SeedDataService));

            if (descriptor != null)
                services.Remove(descriptor);
        });

        builder.UseEnvironment("Test");
    }

    public async Task InitialiseDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await SeedDefaultDataAsync(context);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.ValidationResults.RemoveRange(context.ValidationResults);
        context.ValidationJobs.RemoveRange(context.ValidationJobs);
        context.Sboms.RemoveRange(context.Sboms);
        context.SbomFeatures.RemoveRange(context.SbomFeatures);
        context.SbomProfiles.RemoveRange(context.SbomProfiles);
        context.TeamMembers.RemoveRange(context.TeamMembers);
        context.Products.RemoveRange(context.Products);
        context.Teams.RemoveRange(context.Teams);

        await context.SaveChangesAsync();

        // Re-seed defaults after reset
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
