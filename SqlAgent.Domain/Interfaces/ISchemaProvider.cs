using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Interfaces;

public interface ISchemaProvider
{
    bool EntityExists(string logicalName);
    bool FieldExists(string entityLogical, string fieldLogical);
    bool MetricExists(string metricLogical);

    // Las dos funciones más peligrosas/importantes del motor:
    string ResolvePhysicalTable(string logicalName);
    string ResolvePhysicalField(string entityLogical, string fieldLogical);

    string GetCategory(string logicalName);
}

public interface IRelationshipResolver
{
    // Usado por el CardinalityGuard para evitar JOINs que multipliquen filas
    bool HasBridgeBetween(string factA, string factB, IEnumerable<string> allEntities);
}

public interface ICatalogRepository
{
    // Abstracción para leer desde CatalogDbContext
    Task<Entities.Profile> GetProfileAsync(Guid profileId);
    Task<IEnumerable<Entities.Entity>> GetEntitiesAsync(Guid versionId);
    Task<IEnumerable<Entities.Field>> GetFieldsAsync(Guid entityId);
}