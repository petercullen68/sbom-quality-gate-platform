using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Sbom> Sboms => Set<Sbom>();
    public DbSet<ValidationJob> ValidationJobs => Set<ValidationJob>();
    public DbSet<ValidationResult> ValidationResults => Set<ValidationResult>();
    public DbSet<SbomFeature> SbomFeatures => Set<SbomFeature>();
    public DbSet<SbomProfile> SbomProfiles => Set<SbomProfile>();
    public DbSet<ConformancePolicy> ConformancePolicies => Set<ConformancePolicy>();
    public DbSet<PolicyTier> PolicyTiers => Set<PolicyTier>();
    public DbSet<PolicyRule> PolicyRules => Set<PolicyRule>();
    public DbSet<PolicyEvaluationResult> PolicyEvaluationResults => Set<PolicyEvaluationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).IsRequired();

            entity.HasIndex(x => x.Name).IsUnique();

            entity.HasMany(x => x.Products)
                .WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Members)
                .WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).IsRequired();

            entity.HasIndex(x => new { x.TeamId, x.Name }).IsUnique();

            entity.HasMany(x => x.Sboms)
                .WithOne(x => x.Product)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.TeamId });
        });

        modelBuilder.Entity<Sbom>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SbomJson)
                .HasColumnType("jsonb");
            
            entity.Property(x => x.SbomXml)
                .HasColumnType("text")
                .IsRequired(false);
        });

        modelBuilder.Entity<ValidationJob>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<ValidationResult>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReportJson)
                .HasColumnType("jsonb");
            
            entity.Property(x => x.DeprecationWarnings).HasColumnType("text[]");

            entity.HasOne(x => x.ValidationJob)
                .WithMany()
                .HasForeignKey(x => x.ValidationJobId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ValidationJobId)
                .IsUnique();
        });

        modelBuilder.Entity<SbomFeature>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Feature).IsUnique();

            entity.Property(x => x.Feature).IsRequired();
            entity.Property(x => x.Category).IsRequired();
        });

        modelBuilder.Entity<SbomProfile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name).IsUnique();

            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Description).IsRequired();
        });
        
        modelBuilder.Entity<ConformancePolicy>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.SpecType).IsRequired();
            entity.Property(x => x.MinSpecVersion).IsRequired();

            // At most one scope FK set — enforced in application layer
            entity.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyTier>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).IsRequired();

            entity.HasOne(x => x.Policy)
                .WithMany(x => x.Tiers)
                .HasForeignKey(x => x.PolicyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyRule>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.JsonPath).IsRequired();

            entity.HasOne(x => x.Policy)
                .WithMany(x => x.Rules)
                .HasForeignKey(x => x.PolicyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Tier)
                .WithMany(x => x.Rules)
                .HasForeignKey(x => x.TierId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyEvaluationResult>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ViolationsJson)
                .HasColumnType("jsonb");

            entity.HasOne(x => x.ValidationResult)
                .WithMany()
                .HasForeignKey(x => x.ValidationResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ValidationResultId)
                .IsUnique();
        });
    }
}
