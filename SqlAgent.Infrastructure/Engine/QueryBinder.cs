// SqlAgent.Infrastructure/Engine/QueryBinder.cs
using System;
using System.Linq;
using System.Collections.Generic;
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
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        _compiler = new SqlServerCompiler();
    }

    public SqlResult Bind(QueryModel intent)
    {
        // 1. Tabla principal con alias
        var mainPhysical = _schema.ResolvePhysicalTable(intent.EntityLogical);
        var mainAlias = _schema.GetAlias(intent.EntityLogical);
        var query = new Query($"{mainPhysical} AS {mainAlias}");

        // 2. SELECT
        if (intent.FieldsLogical is { Count: > 0 })
        {
            foreach (var fieldLogical in intent.FieldsLogical)
            {
                var parts = fieldLogical.Split('.', 2);
                var entity = parts.Length > 1 ? parts[0] : intent.EntityLogical;
                var field = parts.Length > 1 ? parts[1] : parts[0];
                var alias = _schema.GetAlias(entity);
                var physical = _schema.ResolvePhysicalField(entity, field);
                query.Select($"{alias}.{physical} AS {field}");
            }
        }
        else
        {
            query.Select($"{mainAlias}.*");
        }

        // 3. MÉTRICAS
        if (!string.IsNullOrEmpty(intent.MetricLogical))
        {
            if (intent.MetricLogical.Equals("Revenue", StringComparison.OrdinalIgnoreCase))
            {
                var odAlias = _schema.GetAlias("OrderDetail");
                var up = _schema.ResolvePhysicalField("OrderDetail", "UnitPrice");
                var qty = _schema.ResolvePhysicalField("OrderDetail", "Quantity");
                query.SelectRaw($"SUM({odAlias}.{up} * {odAlias}.{qty}) AS Revenue");
            }
        }

        // 4. JOINs
        if (intent.JoinsLogical is { Count: > 0 })
        {
            foreach (var join in intent.JoinsLogical)
            {
                var targetPhysical = _schema.ResolvePhysicalTable(join.ToEntityLogical);
                var targetAlias = _schema.GetAlias(join.ToEntityLogical);
                var condition = _relationships.BuildJoinCondition(
                    join.FromEntityLogical, join.ToEntityLogical);

                if (join.JoinType.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                    query.LeftJoin($"{targetPhysical} AS {targetAlias}", j => j.WhereRaw(condition));
                else
                    query.Join($"{targetPhysical} AS {targetAlias}", j => j.WhereRaw(condition));
            }
        }

        // 5. WHERE
        if (intent.Filters is { Count: > 0 })
        {
            foreach (var filter in intent.Filters)
            {
                var alias = _schema.GetAlias(filter.EntityLogical);
                var physical = _schema.ResolvePhysicalField(filter.EntityLogical, filter.FieldLogical);
                query.Where($"{alias}.{physical}", filter.Operator, filter.Value);
            }
        }

        // 6. GROUP BY — soporta "EntityLogical.FieldLogical" y "FieldLogical"
        if (intent.GroupByLogical is { Count: > 0 })
        {
            foreach (var groupField in intent.GroupByLogical)
            {
                var parts = groupField.Split('.', 2);
                var entity = parts.Length > 1 ? parts[0] : intent.EntityLogical;
                var field = parts.Length > 1 ? parts[1] : parts[0];
                var alias = _schema.GetAlias(entity);
                var physical = _schema.ResolvePhysicalField(entity, field);
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

        // 8. TOP
        if (intent.Top.HasValue)
            query.Limit(intent.Top.Value);

        // 9. Compilar y parchear SqlKata 4.x
        var result = _compiler.Compile(query);
        return ApplySqlServerFixes(result, intent);
    }

    /// <summary>
    /// SqlKata 4.x genera OFFSET/FETCH con un ORDER BY (SELECT 0) espurio
    /// cuando hay GROUP BY + Limit sin un ORDER BY real.
    /// Este método lo reemplaza por SELECT TOP N.
    /// </summary>
    private static SqlResult ApplySqlServerFixes(SqlResult result, QueryModel intent)
    {
        if (!intent.Top.HasValue || intent.OrderBy is not null)
            return result;

        var sql = result.Sql;
        var offsetIdx = sql.IndexOf("ORDER BY (SELECT 0)", StringComparison.OrdinalIgnoreCase);

        if (offsetIdx < 0)
            return result;

        // Quitar ORDER BY espurio + OFFSET/FETCH
        sql = sql[..offsetIdx].TrimEnd();

        // Inyectar TOP N después del SELECT
        sql = sql.Replace("SELECT ", $"SELECT TOP {intent.Top.Value} ",
            StringComparison.OrdinalIgnoreCase);

        // Quitar bindings de OFFSET/FETCH que ya no aplican
        result.Sql = sql;
        result.NamedBindings.Remove("p0");
        result.NamedBindings.Remove("p1");

        return result;
    }
}