using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Services;

public record EntityData(
    string PhysicalName,
    string Alias,
    string Category,
    string? DefaultGrainFields,
    Dictionary<string, string> PhysicalFields
);

public record RelationshipData(
    string FromEntityLogical,
    string FromFieldLogical,
    string ToEntityLogical,
    string ToFieldLogical,
    string JoinType
);

public class ProfileService : ISchemaProvider, IRelationshipResolver
{
    private readonly Dictionary<string, EntityData> _entities;
    private readonly Dictionary<(string, string), RelationshipData> _relationships;
    public Guid VersionId { get; }

    private ProfileService(
        Dictionary<string, EntityData> entities,
        Dictionary<(string, string), RelationshipData> relationships,
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
            var fieldMap = fields.ToDictionary(
                f => f.LogicalName,
                f => f.PhysicalName,
                StringComparer.OrdinalIgnoreCase);

            entityMap[entity.LogicalName] = new EntityData(
                entity.PhysicalName,
                entity.Alias,
                entity.Category,
                string.IsNullOrWhiteSpace(entity.DefaultGrainFields)
                    ? null
                    : entity.DefaultGrainFields,
                fieldMap);
        }

        var rels = await catalog.GetRelationshipsAsync(versionId);
        var relationshipMap = new Dictionary<(string, string), RelationshipData>();

        foreach (var rel in rels.Where(r => r.IsApproved))
        {
            var data = new RelationshipData(
                rel.FromEntityLogical, rel.FromFieldLogical,
                rel.ToEntityLogical, rel.ToFieldLogical,
                rel.JoinType);

            var key = (rel.FromEntityLogical, rel.ToEntityLogical);
            var reverseKey = (rel.ToEntityLogical, rel.FromEntityLogical);

            if (!relationshipMap.ContainsKey(key)) relationshipMap[key] = data;
            if (!relationshipMap.ContainsKey(reverseKey)) relationshipMap[reverseKey] = data;
        }

        return new ProfileService(entityMap, relationshipMap, versionId);
    }

    // ── ISchemaProvider ──
    public string ResolvePhysicalTable(string logicalName) =>
        _entities.TryGetValue(logicalName, out var data)
            ? data.PhysicalName
            : throw new InvalidOperationException(
                $"Entidad '{logicalName}' no existe en el perfil activo (VersionId: {VersionId}).");

    public string ResolvePhysicalField(string entityLogical, string fieldLogical)
    {
        if (!_entities.TryGetValue(entityLogical, out var e))
            throw new InvalidOperationException($"Entidad '{entityLogical}' no existe.");
        if (!e.PhysicalFields.TryGetValue(fieldLogical, out var physical))
            throw new InvalidOperationException(
                $"Campo '{fieldLogical}' no existe en '{entityLogical}'. " +
                $"Disponibles: {string.Join(", ", e.PhysicalFields.Keys)}");
        return physical;
    }

    public string GetAlias(string logicalName) =>
        _entities.TryGetValue(logicalName, out var data)
            ? data.Alias
            : throw new InvalidOperationException($"Entidad '{logicalName}' no existe.");

    public string GetCategory(string logicalName) =>
        _entities.TryGetValue(logicalName, out var data) ? data.Category : "dimension";

    public string? GetDefaultGrainFields(string logicalName) =>
        _entities.TryGetValue(logicalName, out var data) ? data.DefaultGrainFields : null;

    public bool EntityExists(string logicalName) => _entities.ContainsKey(logicalName);
    public bool FieldExists(string e, string f) => _entities.TryGetValue(e, out var ed) && ed.PhysicalFields.ContainsKey(f);

    // ── IRelationshipResolver ──
    public string BuildJoinCondition(string fromLogical, string toLogical)
    {
        if (!_relationships.TryGetValue((fromLogical, toLogical), out var rel))
            throw new InvalidOperationException(
                $"Sin relación aprobada entre '{fromLogical}' y '{toLogical}'.");

        var fromAlias = GetAlias(rel.FromEntityLogical);
        var toAlias = GetAlias(rel.ToEntityLogical);
        var fromField = ResolvePhysicalField(rel.FromEntityLogical, rel.FromFieldLogical);
        var toField = ResolvePhysicalField(rel.ToEntityLogical, rel.ToFieldLogical);

        return $"{fromAlias}.{fromField} = {toAlias}.{toField}";
    }

    public bool RelationshipExists(string fromLogical, string toLogical) =>
        _relationships.ContainsKey((fromLogical, toLogical));
}