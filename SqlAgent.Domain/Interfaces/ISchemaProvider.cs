// SqlAgent.Domain/Interfaces/ISchemaProvider.cs
using SqlAgent.Domain.Entities;

namespace SqlAgent.Domain.Interfaces;

public interface ISchemaProvider
{
    bool EntityExists(string logicalName);
    bool FieldExists(string entityLogical, string fieldLogical);
    string ResolvePhysicalTable(string logicalName);
    string ResolvePhysicalField(string entityLogical, string fieldLogical);
    string GetAlias(string logicalName);
    string GetCategory(string logicalName);
    string? GetDefaultGrainFields(string logicalName); // plural, nullable
}

public interface IRelationshipResolver
{
    string BuildJoinCondition(string fromLogical, string toLogical); // construye el ON físico
    bool RelationshipExists(string fromLogical, string toLogical);
}

public interface ICatalogRepository
{
    Task<Profile> GetProfileAsync(Guid profileId);
    Task<ProfileVersion> GetVersionAsync(Guid versionId);
    Task<IEnumerable<Entity>> GetEntitiesAsync(Guid versionId);
    Task<IEnumerable<Field>> GetFieldsAsync(Guid entityId);
    Task<IEnumerable<Relationship>> GetRelationshipsAsync(Guid versionId);
}