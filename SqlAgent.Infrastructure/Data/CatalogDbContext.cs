using Microsoft.EntityFrameworkCore;
using SqlAgent.Domain.Entities;

namespace SqlAgent.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileVersion> Versions => Set<ProfileVersion>(); // <--- Actualizado
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Synonym> Synonyms => Set<Synonym>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<Policy> Policies => Set<Policy>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Profile
        modelBuilder.Entity<Profile>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.TenantId).IsRequired().HasMaxLength(50);
            // Referencia actualizada a ProfileVersion
            b.HasMany(p => p.Versions).WithOne(v => v.Profile).HasForeignKey(v => v.ProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        // ProfileVersion
        modelBuilder.Entity<ProfileVersion>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.VersionName).IsRequired().HasMaxLength(50);
            b.Property(v => v.Status).IsRequired().HasMaxLength(20);
            b.HasOne(v => v.DataSource).WithOne(d => d.Version).HasForeignKey<DataSource>(d => d.VersionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(v => v.Entities).WithOne(e => e.Version).HasForeignKey(e => e.VersionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(v => v.Relationships).WithOne(r => r.Version).HasForeignKey(r => r.VersionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(v => v.Metrics).WithOne(m => m.Version).HasForeignKey(m => m.VersionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(v => v.Policies).WithOne(p => p.Version).HasForeignKey(p => p.VersionId).OnDelete(DeleteBehavior.Cascade);
        });

        // DataSource
        modelBuilder.Entity<DataSource>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.ConnectionStringName).IsRequired().HasMaxLength(200);
            b.Property(d => d.Engine).IsRequired().HasMaxLength(50);
        });

        // Entity
        modelBuilder.Entity<Entity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.LogicalName).IsRequired().HasMaxLength(100);
            b.Property(e => e.PhysicalName).IsRequired().HasMaxLength(100);
            b.HasIndex(e => new { e.VersionId, e.LogicalName }).IsUnique();
            b.HasMany(e => e.Fields).WithOne(f => f.Entity).HasForeignKey(f => f.EntityId).OnDelete(DeleteBehavior.Cascade);
        });

        // Field
        modelBuilder.Entity<Field>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.LogicalName).IsRequired().HasMaxLength(100);
            b.Property(f => f.PhysicalName).IsRequired().HasMaxLength(100);
            b.Property(f => f.DataType).IsRequired().HasMaxLength(50);
            b.HasIndex(f => new { f.EntityId, f.LogicalName }).IsUnique();
            b.HasMany(f => f.Synonyms).WithOne(s => s.Field).HasForeignKey(s => s.FieldId).OnDelete(DeleteBehavior.Cascade);
        });

        // Synonym
        modelBuilder.Entity<Synonym>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Term).IsRequired().HasMaxLength(100);
            b.HasIndex(s => s.Term);
        });

        // Relationship
        modelBuilder.Entity<Relationship>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.SourceLogicalName).IsRequired().HasMaxLength(100);
            b.Property(r => r.TargetLogicalName).IsRequired().HasMaxLength(100);
            b.Property(r => r.JoinCondition).IsRequired();
        });

        // Metric
        modelBuilder.Entity<Metric>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).IsRequired().HasMaxLength(100);
            b.Property(m => m.Formula).IsRequired();
        });

        // Policy
        modelBuilder.Entity<Policy>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.RuleDefinition).IsRequired();
        });
    }
}