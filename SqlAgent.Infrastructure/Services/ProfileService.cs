using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Services;

public record EntityData(
    string PhysicalName,
    string Alias,
    string Category,
    string DefaultGrainFields,
    Dictionary<string, string> PhysicalFields
);

public class ProfileService : ISchemaProvider, IRelationshipResolver
{
    private readonly Dictionary<string, EntityData> _entities;
    private readonly Dictionary<(string, string), string> _relationships;
    public Guid VersionId { get; }

    private ProfileService(
        Dictionary<string, EntityData> entities,
        Dictionary<(string, string), string> relationships,
        Guid versionId)
    {
        _entities = entities;
        _relationships = relationships;
        VersionId = versionId;
    }

    public static async Task<ProfileService> LoadAsync(Guid versionId, ICatalogRepository catalog)
    {
        var entities = await catalog.GetEntitiesAsync(versionId);
        var entityMap = new Dictionary<string, EntityData>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            var fields = await catalog.GetFieldsAsync(entity.Id);
            var fieldMap = fields.ToDictionary(f => f.LogicalName, f => f.PhysicalName, StringComparer.OrdinalIgnoreCase);

            entityMap[entity.LogicalName] = new EntityData(
                entity.PhysicalName,
                entity.Alias,
                entity.Category,
                entity.DefaultGrainFields,
                fieldMap
            );
        }

        var relationships = await catalog.GetRelationshipsAsync(versionId);
        var relationshipMap = new Dictionary<(string, string), string>();

        foreach (var rel in relationships)
        {
            // CORRECCIÓN: Usando FromEntityLogical y ToEntityLogical
            var key = (rel.FromEntityLogical, rel.ToEntityLogical);
            if (!relationshipMap.ContainsKey(key))
                relationshipMap.Add(key, rel.JoinCondition);

            var reverseKey = (rel.ToEntityLogical, rel.FromEntityLogical);
            if (!relationshipMap.ContainsKey(reverseKey))
                relationshipMap.Add(reverseKey, rel.JoinCondition);
        }

        return new ProfileService(entityMap, relationshipMap, versionId);
    }

    public string ResolvePhysicalTable(string logicalName) =>
        _entities.TryGetValue(logicalName, out var data) ? data.PhysicalName : throw new Exception($"Entidad {logicalName} no existe.");

    public string ResolvePhysicalField(string entityLogical, string fieldLogical) =>
        _entities.TryGetValue(entityLogical, out var e) && e.PhysicalFields.TryGetValue(fieldLogical, out var p)
            ? p : throw new Exception($"Campo {fieldLogical} no existe en {entityLogical}.");

    public string GetDefaultGrainField(string entityLogical) =>
        _entities.TryGetValue(entityLogical, out var data) ? data.DefaultGrainFields : "";

    public string GetJoinCondition(string sourceLogical, string targetLogical) =>
        _relationships.TryGetValue((sourceLogical, targetLogical), out var condition) ? condition : throw new Exception($"Sin relación entre {sourceLogical} y {targetLogical}.");

    public bool EntityExists(string logicalName) => _entities.ContainsKey(logicalName);
    public bool FieldExists(string entityLogical, string fieldLogical) => _entities.TryGetValue(entityLogical, out var e) && e.PhysicalFields.ContainsKey(fieldLogical);
}