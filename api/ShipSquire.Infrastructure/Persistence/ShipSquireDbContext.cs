using Microsoft.EntityFrameworkCore;
using ShipSquire.Domain.Entities;

namespace ShipSquire.Infrastructure.Persistence;

public class ShipSquireDbContext : DbContext
{
    public ShipSquireDbContext(DbContextOptions<ShipSquireDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Runbook> Runbooks => Set<Runbook>();
    public DbSet<RunbookSection> RunbookSections => Set<RunbookSection>();
    public DbSet<RunbookVariable> RunbookVariables => Set<RunbookVariable>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentTimelineEntry> IncidentTimelineEntries => Set<IncidentTimelineEntry>();
    public DbSet<Postmortem> Postmortems => Set<Postmortem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.AuthProvider).HasMaxLength(50);
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Slug }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RepoProvider).HasMaxLength(50);
            entity.Property(e => e.RepoOwner).HasMaxLength(255);
            entity.Property(e => e.RepoName).HasMaxLength(255);
            entity.Property(e => e.RepoUrl).HasMaxLength(500);
            entity.Property(e => e.DefaultBranch).HasMaxLength(100);
            entity.Property(e => e.PrimaryLanguage).HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Services)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Runbook configuration
        modelBuilder.Entity<Runbook>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Runbooks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Runbooks)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RunbookSection configuration
        modelBuilder.Entity<RunbookSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BodyMarkdown).IsRequired();

            entity.HasOne(e => e.Runbook)
                .WithMany(r => r.Sections)
                .HasForeignKey(e => e.RunbookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RunbookVariable configuration
        modelBuilder.Entity<RunbookVariable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.Runbook)
                .WithMany(r => r.Variables)
                .HasForeignKey(e => e.RunbookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Incident configuration
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Incidents)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Incidents)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Runbook)
                .WithMany(r => r.Incidents)
                .HasForeignKey(e => e.RunbookId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // IncidentTimelineEntry configuration
        modelBuilder.Entity<IncidentTimelineEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BodyMarkdown).IsRequired();

            entity.HasOne(e => e.Incident)
                .WithMany(i => i.TimelineEntries)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Postmortem configuration
        modelBuilder.Entity<Postmortem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IncidentId).IsUnique();

            entity.HasOne(e => e.Incident)
                .WithOne(i => i.Postmortem)
                .HasForeignKey<Postmortem>(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
            {
                entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
