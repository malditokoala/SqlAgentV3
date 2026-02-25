using Microsoft.EntityFrameworkCore;
using SqlAgent.Domain.Entities;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Infrastructure.Data;

namespace SqlAgent.Infrastructure.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Profile> GetProfileAsync(Guid profileId)
    {
        return await _context.Profiles
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == profileId)
            ?? throw new KeyNotFoundException(
                $"Perfil '{profileId}' no existe en el catálogo.");
    }

    public async Task<ProfileVersion> GetVersionAsync(Guid versionId)
    {
        return await _context.Versions
            .Include(v => v.DataSource)
            .FirstOrDefaultAsync(v => v.Id == versionId)
            ?? throw new KeyNotFoundException(
                $"Version '{versionId}' no existe en el catálogo.");
    }

    public async Task<IEnumerable<Entity>> GetEntitiesAsync(Guid versionId)
    {
        return await _context.Entities
            .Where(e => e.VersionId == versionId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Field>> GetFieldsAsync(Guid entityId)
    {
        return await _context.Fields
            .Where(f => f.EntityId == entityId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Relationship>> GetRelationshipsAsync(Guid versionId)
    {
        // Solo relaciones aprobadas — el motor nunca debe ver relaciones pendientes
        return await _context.Relationships
            .Where(r => r.VersionId == versionId && r.IsApproved)
            .ToListAsync();
    }
}