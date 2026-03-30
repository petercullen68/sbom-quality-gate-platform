using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Infrastructure.Persistence;

namespace SbomQualityGate.Infrastructure.Seed;


public class SeedDataService(
    IServiceProvider serviceProvider,
    ILogger<SeedDataService> logger) : IHostedService
{
    private const string DefaultTeamName = "Default Team";
    private const string DefaultProductName = "Default Product";

    // Event ID range: 11–20
    
    private static readonly Action<ILogger, Exception?> SeedDataAlreadyPresent =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(11, nameof(SeedDataService)),
            "Seed data already present — skipping.");

    private static readonly Action<ILogger, string, Guid, Exception?> SeedingTeam =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Information,
            new EventId(12, nameof(SeedDataService)),
            "Seeding default team '{Name}' ({Id})");

    private static readonly Action<ILogger, string, Guid, Guid, Exception?> SeedingProduct =
        LoggerMessage.Define<string, Guid, Guid>(
            LogLevel.Information,
            new EventId(13, nameof(SeedDataService)),
            "Seeding default product '{Name}' ({Id}) for team {TeamId}");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var teamExists = await context.Teams.AnyAsync(cancellationToken);
        var productExists = await context.Products.AnyAsync(cancellationToken);

        if (teamExists && productExists)
        {
            SeedDataAlreadyPresent(logger, null);
            return;
        }

        await unitOfWork.ExecuteAsync(async () =>
        {
            Guid teamId;

            if (!teamExists)
            {
                var team = new Team
                {
                    Id = Guid.NewGuid(),
                    Name = DefaultTeamName,
                    CreatedAt = DateTime.UtcNow
                };

                context.Teams.Add(team);
                teamId = team.Id;

                SeedingTeam(logger, DefaultTeamName, teamId, null);
            }
            else
            {
                teamId = await context.Teams
                    .Where(x => x.Name == DefaultTeamName)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!productExists && teamId != Guid.Empty)
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    TeamId = teamId,
                    Name = DefaultProductName,
                    CreatedAt = DateTime.UtcNow
                };

                context.Products.Add(product);

                SeedingProduct(logger, DefaultProductName, product.Id, teamId, null);
            }

        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
