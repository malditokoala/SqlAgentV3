using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqlAgent.Domain.Entities;

namespace SqlAgent.Domain.Interfaces;

public interface ISchemaProvider
{
    bool EntityExists(string logicalName);
    bool FieldExists(string entityLogical, string fieldLogical);
    string ResolvePhysicalTable(string logicalName);
    string ResolvePhysicalField(string entityLogical, string fieldLogical);
    string GetDefaultGrainField(string entityLogical);
}

public interface IRelationshipResolver
{
    string GetJoinCondition(string sourceLogical, string targetLogical);
}

public interface ICatalogRepository
{
    Task<IEnumerable<Entity>> GetEntitiesAsync(Guid versionId);
    Task<IEnumerable<Field>> GetFieldsAsync(Guid entityId);
    // Método necesario para cargar relaciones
    Task<IEnumerable<Relationship>> GetRelationshipsAsync(Guid versionId);
}