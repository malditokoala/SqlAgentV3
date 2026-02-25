using Microsoft.EntityFrameworkCore;
using SqlAgent.Domain.Entities;

namespace SqlAgent.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileVersion> Versions => Set<ProfileVersion>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Synonym> Synonyms => Set<Synonym>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<Policy> Policies => Set<Policy>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.TenantId).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<ProfileVersion>(b =>
        {
            b.HasKey(v => v.Id);
            b.Property(v => v.VersionName).IsRequired().HasMaxLength(50);
            b.Property(v => v.Status).IsRequired().HasMaxLength(20);
            b.HasOne(v => v.Profile)
             .WithMany(p => p.Versions)
             .HasForeignKey(v => v.ProfileId);
        });

        modelBuilder.Entity<DataSource>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.ConnectionStringName).IsRequired().HasMaxLength(100);
            b.Property(d => d.Engine).IsRequired().HasMaxLength(20);
            b.HasOne(d => d.Version)
             .WithOne(v => v.DataSource)
             .HasForeignKey<DataSource>(d => d.VersionId);
        });

        modelBuilder.Entity<Entity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.LogicalName).IsRequired().HasMaxLength(100);
            b.Property(e => e.PhysicalName).IsRequired().HasMaxLength(200);
            b.Property(e => e.Alias).HasMaxLength(10);
            b.Property(e => e.Category).HasMaxLength(20);
            b.Property(e => e.DefaultGrainFields).HasMaxLength(500); // ← 500
            b.HasIndex(e => new { e.VersionId, e.LogicalName }).IsUnique();
            b.HasOne(e => e.Version)
             .WithMany(v => v.Entities)
             .HasForeignKey(e => e.VersionId);
        });

        modelBuilder.Entity<Field>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.LogicalName).IsRequired().HasMaxLength(100);
            b.Property(f => f.PhysicalName).IsRequired().HasMaxLength(200);
            b.Property(f => f.DataType).HasMaxLength(50);
            b.HasOne(f => f.Entity)
             .WithMany(e => e.Fields)
             .HasForeignKey(f => f.EntityId);
        });

        modelBuilder.Entity<Relationship>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.FromEntityLogical).IsRequired().HasMaxLength(100);
            b.Property(r => r.FromFieldLogical).IsRequired().HasMaxLength(100);
            b.Property(r => r.ToEntityLogical).IsRequired().HasMaxLength(100);
            b.Property(r => r.ToFieldLogical).IsRequired().HasMaxLength(100);
            b.Property(r => r.JoinType).HasMaxLength(20);
            // ← JoinCondition eliminado
            b.HasOne(r => r.Version)
             .WithMany(v => v.Relationships)
             .HasForeignKey(r => r.VersionId);
        });

        modelBuilder.Entity<Metric>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.LogicalName).IsRequired().HasMaxLength(100);
            b.Property(m => m.ExpressionAst).IsRequired();
            b.Property(m => m.Alias).IsRequired().HasMaxLength(50);
            b.Property(m => m.ValidAggregations).HasMaxLength(100);
            b.HasOne(m => m.Version)
             .WithMany(v => v.Metrics)
             .HasForeignKey(m => m.VersionId);
        });

        modelBuilder.Entity<Policy>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.AllowedOperations).HasMaxLength(200);
            b.Property(p => p.GlobalDenyFields).HasMaxLength(500);
            b.HasOne(p => p.Version)
             .WithMany(v => v.Policies)
             .HasForeignKey(p => p.VersionId);
        });

        modelBuilder.Entity<Synonym>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Term).IsRequired().HasMaxLength(100);
            b.Property(s => s.TargetType).IsRequired().HasMaxLength(20);
            b.Property(s => s.TargetLogicalName).IsRequired().HasMaxLength(100);
            b.Property(s => s.EntityContext).HasMaxLength(100);
            b.Property(s => s.Confidence).HasPrecision(3, 2);
            b.HasOne(s => s.Version)
             .WithMany(v => v.Synonyms)
             .HasForeignKey(s => s.VersionId);
        });
    }
}