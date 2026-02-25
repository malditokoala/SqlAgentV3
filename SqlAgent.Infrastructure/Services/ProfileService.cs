using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Services;

// Datos cacheados internamente para acceso ultrarrápido
public record EntityData(string PhysicalName, string DefaultGrainFields, Dictionary<string, string> PhysicalFields);

public class ProfileService : ISchemaProvider, IRelationshipResolver
{
    private readonly Dictionary<string, EntityData> _entities;
    public Guid VersionId { get; }

    // Constructor privado. Forzamos la creación mediante el Factory Method 'LoadAsync'
    private ProfileService(Dictionary<string, EntityData> entities, Guid versionId)
    {
        _entities = entities;
        VersionId = versionId;
    }

    public static async Task<ProfileService> LoadAsync(Guid versionId, ICatalogRepository catalog)
    {
        var entities = await catalog.GetEntitiesAsync(versionId);
        var entityMap = new Dictionary<string, EntityData>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            var fields = await catalog.GetFieldsAsync(entity.Id);

            // Mapeo lógico -> físico de los campos de esta entidad
            var fieldMap = fields.ToDictionary(
                f => f.LogicalName,
                f => f.PhysicalName,
                StringComparer.OrdinalIgnoreCase
            );

            // Mapeo lógico -> datos de la entidad
            entityMap[entity.LogicalName] = new EntityData(
                entity.PhysicalName,
                "", // Aquí enlazaremos DefaultGrainFields más adelante
                fieldMap
            );
        }

        return new ProfileService(entityMap, versionId);
    }

    // ── ISchemaProvider ──
    public string ResolvePhysicalTable(string logicalName)
    {
        if (_entities.TryGetValue(logicalName, out var data))
            return data.PhysicalName;

        throw new Exception($"La entidad lógica '{logicalName}' no existe en el perfil activo.");
    }

    public string ResolvePhysicalField(string entityLogical, string fieldLogical)
    {
        if (_entities.TryGetValue(entityLogical, out var entityData) &&
            entityData.PhysicalFields.TryGetValue(fieldLogical, out var physicalField))
        {
            return physicalField;
        }

        throw new Exception($"El campo lógico '{fieldLogical}' no existe en la entidad '{entityLogical}'.");
    }

    public bool EntityExists(string logicalName) => _entities.ContainsKey(logicalName);

    public bool FieldExists(string entityLogical, string fieldLogical) =>
        _entities.TryGetValue(entityLogical, out var e) && e.PhysicalFields.ContainsKey(fieldLogical);

    // Stubs para completar en los siguientes pasos
    public bool MetricExists(string metricLogical) => false;
    public string GetCategory(string logicalName) => "dimension";

    // ── IRelationshipResolver ──
    public bool HasBridgeBetween(string factA, string factB, IEnumerable<string> allEntities)
    {
        return false; // Será inyectado con los metadatos de Relationships para el CardinalityGuard
    }
}