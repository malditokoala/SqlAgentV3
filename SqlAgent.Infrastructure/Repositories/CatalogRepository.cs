using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SqlAgent.Domain.Entities;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Infrastructure.Data;

namespace SqlAgent.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio para el acceso a los metadatos del catálogo.
/// Se comunica directamente con el CatalogDbContext.
/// </summary>
public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Obtiene un perfil específico por su identificador.
    /// </summary>
    public async Task<Profile> GetProfileAsync(Guid profileId)
    {
        var profile = await _context.Profiles
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == profileId);

        if (profile == null)
            throw new Exception($"No se encontró el perfil con ID: {profileId}");

        return profile;
    }

    /// <summary>
    /// Recupera todas las entidades (tablas) asociadas a una versión específica.
    /// </summary>
    public async Task<IEnumerable<Entity>> GetEntitiesAsync(Guid versionId)
    {
        return await _context.Entities
            .Where(e => e.VersionId == versionId)
            .ToListAsync();
    }

    /// <summary>
    /// Recupera todos los campos (columnas) de una entidad específica.
    /// </summary>
    public async Task<IEnumerable<Field>> GetFieldsAsync(Guid entityId)
    {
        return await _context.Fields
            .Where(f => f.EntityId == entityId)
            .ToListAsync();
    }

    /// <summary>
    /// Recupera las relaciones lógicas definidas para una versión.
    /// Crucial para la resolución dinámica de JOINs en la v3.0.
    /// </summary>
    public async Task<IEnumerable<Relationship>> GetRelationshipsAsync(Guid versionId)
    {
        return await _context.Relationships
            .Where(r => r.VersionId == versionId)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene una versión específica por su ID.
    /// </summary>
    public async Task<ProfileVersion> GetVersionAsync(Guid versionId)
    {
        var version = await _context.Versions
            .Include(v => v.DataSource)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        if (version == null)
            throw new Exception($"No se encontró la versión con ID: {versionId}");

        return version;
    }
}