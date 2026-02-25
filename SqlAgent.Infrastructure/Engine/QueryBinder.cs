using System;
using System.Linq;
using SqlKata;
using SqlKata.Compilers;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Engine;

public class QueryBinder
{
    private readonly ISchemaProvider _schema;
    private readonly IRelationshipResolver _relationships;
    private readonly SqlServerCompiler _compiler;

    public QueryBinder(ISchemaProvider schema, IRelationshipResolver relationships)
    {
        _schema = schema;
        _relationships = relationships;
        _compiler = new SqlServerCompiler();
    }

    public SqlResult Bind(QueryModel intent)
    {
        // 1. Tabla principal con alias
        var mainPhysical = _schema.ResolvePhysicalTable(intent.EntityLogical);
        var mainAlias = _schema.GetAlias(intent.EntityLogical);
        var query = new Query($"{mainPhysical} AS {mainAlias}");

        // 2. TOP — un solo bloque
        if (intent.Top.HasValue)
        {
            if (intent.OrderBy is null)
                query = query.Take(intent.Top.Value);  // genera SELECT TOP N o OFFSET (parcheado abajo)
            else
                query.Limit(intent.Top.Value);         // genera OFFSET/FETCH con ORDER BY
        }

        // 3. SELECT
        if (intent.FieldsLogical is { Count: > 0 })
        {
            foreach (var fieldLogical in intent.FieldsLogical)
            {
                string entityPart, fieldPart;
                if (fieldLogical.Contains('.'))
                {
                    var parts = fieldLogical.Split('.', 2);
                    entityPart = parts[0];
                    fieldPart = parts[1];
                }
                else
                {
                    entityPart = intent.EntityLogical;
                    fieldPart = fieldLogical;
                }

                var alias = _schema.GetAlias(entityPart);
                var physical = _schema.ResolvePhysicalField(entityPart, fieldPart);
                query.Select($"{alias}.{physical} as {fieldPart}");
            }
        }
        else
        {
            query.Select($"{mainAlias}.*");
        }

        // 4. JOINs
        if (intent.JoinsLogical is { Count: > 0 })
        {
            foreach (var join in intent.JoinsLogical)
            {
                var targetPhysical = _schema.ResolvePhysicalTable(join.ToEntityLogical);
                var targetAlias = _schema.GetAlias(join.ToEntityLogical);
                var condition = _relationships.BuildJoinCondition(
                    intent.EntityLogical, join.ToEntityLogical);

                query.Join($"{targetPhysical} AS {targetAlias}", j => j.WhereRaw(condition));
            }
        }

        // 5. WHERE
        if (intent.Filters is { Count: > 0 })
        {
            foreach (var filter in intent.Filters)
            {
                var physicalAlias = _schema.GetAlias(filter.EntityLogical);
                var physicalField = _schema.ResolvePhysicalField(filter.EntityLogical, filter.FieldLogical);
                query.Where($"{physicalAlias}.{physicalField}", filter.Operator, filter.Value);
            }
        }

        // 6. GROUP BY — soporta "EntityLogical.FieldLogical" y "FieldLogical"
        if (intent.GroupByLogical is { Count: > 0 })
        {
            foreach (var groupField in intent.GroupByLogical)
            {
                string entityPart, fieldPart;
                if (groupField.Contains('.'))
                {
                    var parts = groupField.Split('.', 2);
                    entityPart = parts[0];
                    fieldPart = parts[1];
                }
                else
                {
                    entityPart = intent.EntityLogical;
                    fieldPart = groupField;
                }

                var alias = _schema.GetAlias(entityPart);
                var physical = _schema.ResolvePhysicalField(entityPart, fieldPart);
                query.GroupBy($"{alias}.{physical}");
            }
        }

        // 7. ORDER BY — tipo discriminado
        if (intent.OrderBy is not null)
        {
            switch (intent.OrderBy)
            {
                case FieldOrderBy fo:
                    var foAlias = _schema.GetAlias(fo.EntityLogical);
                    var foField = _schema.ResolvePhysicalField(fo.EntityLogical, fo.FieldLogical);
                    if (intent.OrderDescending) query.OrderByDesc($"{foAlias}.{foField}");
                    else query.OrderBy($"{foAlias}.{foField}");
                    break;

                case MetricOrderBy mo:
                    if (intent.OrderDescending) query.OrderByRaw($"{mo.MetricLogical} DESC");
                    else query.OrderByRaw($"{mo.MetricLogical} ASC");
                    break;
            }
        }

        // --- COMPILACIÓN Y PARCHEO SQLKATA 4.X ---
        var result = _compiler.Compile(query);

        // SqlKata 4.x genera OFFSET/FETCH cuando hay GROUP BY + Take().
        // Si hay Top sin OrderBy explícito, reemplazamos por SELECT TOP N.
        if (intent.Top.HasValue && intent.OrderBy is null)
            return PatchTopN(result, intent.Top.Value);

        return result;
    }

    private static SqlResult PatchTopN(SqlResult result, int top)
    {
        // Eliminar "ORDER BY (SELECT 0) OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY"
        var sql = result.Sql;
        var offsetIdx = sql.IndexOf("ORDER BY (SELECT 0)", StringComparison.OrdinalIgnoreCase);

        if (offsetIdx > 0)
            sql = sql[..offsetIdx].TrimEnd();

        // Insertar TOP N después del SELECT
        sql = sql.Replace("SELECT ", $"SELECT TOP {top} ", StringComparison.OrdinalIgnoreCase);

        // Eliminar los bindings de OFFSET/FETCH (@p0, @p1) que ya no aplican
        var cleanBindings = result.NamedBindings
            .Where(kvp => kvp.Key != "p0" && kvp.Key != "p1")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Mutar el objeto de compilación existente en lugar de instanciar uno nuevo
        result.Sql = sql;
        result.Bindings = cleanBindings.Values.ToList();
        result.NamedBindings = cleanBindings;

        return result;
    }
}