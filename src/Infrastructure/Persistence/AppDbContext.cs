using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Sbom> Sboms => Set<Sbom>();
    public DbSet<ValidationJob> ValidationJobs => Set<ValidationJob>();
    public DbSet<ValidationResult> ValidationResults => Set<ValidationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sbom>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SbomJson)
                .HasColumnType("jsonb");
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

            entity.HasOne(x => x.ValidationJob)
                .WithMany()
                .HasForeignKey(x => x.ValidationJobId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ValidationJobId)
                .IsUnique();
        });
    }
}