using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        _context = context;
    }

    public async Task<Profile> GetProfileAsync(Guid profileId)
    {
        var profile = await _context.Profiles
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == profileId);

        if (profile == null)
            throw new Exception($"No se encontró el perfil con ID {profileId}");

        return profile;
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
}