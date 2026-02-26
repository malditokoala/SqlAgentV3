using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlKata;
using SqlKata.Compilers;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SqlAgent.Infrastructure.Engine;

/// <summary>
/// Traduce un QueryModel (Lógico) a un SqlResult (Físico).
/// Implementa los parches de compatibilidad para SQL Server y SqlKata 4.x.
/// </summary>
public class QueryBinder
{
    private readonly ISchemaProvider _schema;
    private readonly IRelationshipResolver _relationships;
    private readonly SqlServerCompiler _compiler;
    private readonly ILogger<QueryBinder>? _logger;

    public QueryBinder(
        ISchemaProvider schema,
        IRelationshipResolver relationships,
        ILogger<QueryBinder>? logger = null)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        _compiler = new SqlServerCompiler();
        _logger = logger;
    }

    public SqlResult Bind(QueryModel intent)
    {
        _logger?.LogDebug("═══ QueryBinder.Bind() INICIO ═══");

        // 0. Resolver tabla principal
        var mainPhysical = _schema.ResolvePhysicalTable(intent.EntityLogical);
        var mainAlias = _schema.GetAlias(intent.EntityLogical);
        var query = new Query($"{mainPhysical} AS {mainAlias}");

        // 1. SELECT (Proyección)
        bool hasSelectColumns = false;

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
                hasSelectColumns = true;
            }
        }

        // 2. METRIC (KPI)
        if (!string.IsNullOrEmpty(intent.MetricLogical))
        {
            if (intent.MetricLogical.Equals("Revenue", StringComparison.OrdinalIgnoreCase))
            {
                var odAlias = _schema.GetAlias("OrderDetail");
                var up = _schema.ResolvePhysicalField("OrderDetail", "UnitPrice");
                var qty = _schema.ResolvePhysicalField("OrderDetail", "Quantity");

                // Usamos interpolación directa con alias seguros para evitar que SqlKata escape de más
                query.SelectRaw($"SUM({odAlias}.{up} * {odAlias}.{qty}) AS Revenue");
                hasSelectColumns = true;
            }
        }

        // GUARD: Evitar SQL vacío si no hay proyección
        if (!hasSelectColumns)
        {
            query.Select($"{mainAlias}.*");
        }

        // 3. JOINs (Inyectados por el JoinPlanner)
        if (intent.JoinsLogical is { Count: > 0 })
        {
            foreach (var join in intent.JoinsLogical)
            {
                var targetTable = _schema.ResolvePhysicalTable(join.ToEntityLogical);
                var targetAlias = _schema.GetAlias(join.ToEntityLogical);
                var condition = _relationships.BuildJoinCondition(join.FromEntityLogical, join.ToEntityLogical);

                _logger?.LogDebug("Aplicando JOIN: {Table} AS {Alias}", targetTable, targetAlias);

                if (join.JoinType.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                    query.LeftJoin($"{targetTable} AS {targetAlias}", j => j.WhereRaw(condition));
                else
                    query.Join($"{targetTable} AS {targetAlias}", j => j.WhereRaw(condition));
            }
        }

        // 4. WHERE (Filtros)
        if (intent.Filters is { Count: > 0 })
        {
            foreach (var filter in intent.Filters)
            {
                var alias = _schema.GetAlias(filter.EntityLogical);
                var physical = _schema.ResolvePhysicalField(filter.EntityLogical, filter.FieldLogical);
                query.Where($"{alias}.{physical}", filter.Operator, filter.Value);
            }
        }

        // 5. GROUP BY
        if (intent.GroupByLogical is { Count: > 0 })
        {
            foreach (var g in intent.GroupByLogical)
            {
                var parts = g.Split('.', 2);
                var entity = parts.Length > 1 ? parts[0] : intent.EntityLogical;
                var field = parts.Length > 1 ? parts[1] : parts[0];
                var alias = _schema.GetAlias(entity);
                var physical = _schema.ResolvePhysicalField(entity, field);
                query.GroupBy($"{alias}.{physical}");
            }
        }

        // 6. ORDER BY
        if (intent.OrderBy != null)
        {
            if (intent.OrderBy is FieldOrderBy fo)
            {
                var alias = _schema.GetAlias(fo.EntityLogical);
                var physical = _schema.ResolvePhysicalField(fo.EntityLogical, fo.FieldLogical);
                if (intent.OrderDescending) query.OrderByDesc($"{alias}.{physical}");
                else query.OrderBy($"{alias}.{physical}");
            }
            else if (intent.OrderBy is MetricOrderBy mo)
            {
                if (intent.OrderDescending) query.OrderByRaw($"{mo.MetricLogical} DESC");
                else query.OrderByRaw($"{mo.MetricLogical} ASC");
            }
        }

        // 7. LIMIT (TOP)
        if (intent.Top.HasValue) query.Limit(intent.Top.Value);

        // Compilación base
        var compiled = _compiler.Compile(query);

        // 8. Aplicar parches críticos para SQL Server y retornar
        var result = ApplySqlServerFixes(compiled, intent, _logger);

        // Verificación final de seguridad
        if (string.IsNullOrWhiteSpace(result.Sql))
        {
            _logger?.LogError("ERROR: El SQL resultó vacío tras los parches.");
            return compiled;
        }

        _logger?.LogDebug("═══ QueryBinder.Bind() FIN ═══");

        return result;
    }

    private static SqlResult ApplySqlServerFixes(SqlResult compiled, QueryModel intent, ILogger? logger)
    {
        if (compiled == null || string.IsNullOrWhiteSpace(compiled.Sql)) return compiled!;

        string sql = compiled.Sql;

        // Si no hay TOP, retornamos el original
        if (!intent.Top.HasValue) return compiled;

        try
        {
            // 1. Quitar OFFSET/FETCH (SqlKata lo agrega al usar Limit con OrderBy)
            // Mejoramos el Regex para que capture incluso si hay parámetros (@p0)
            sql = Regex.Replace(sql, @"\s+OFFSET\s+.*$", "", RegexOptions.IgnoreCase | RegexOptions.Singleline).Trim();

            // 2. Quitar el ordenamiento dummy
            sql = sql.Replace("ORDER BY (SELECT 0)", "", StringComparison.OrdinalIgnoreCase).Trim();

            // 3. Inyectar TOP(N)
            if (!sql.Contains("SELECT TOP", StringComparison.OrdinalIgnoreCase))
            {
                var topValue = $"TOP ({intent.Top.Value}) ";
                if (sql.Contains("SELECT DISTINCT", StringComparison.OrdinalIgnoreCase))
                {
                    sql = Regex.Replace(sql, "DISTINCT", $"DISTINCT {topValue}", RegexOptions.IgnoreCase);
                }
                else
                {
                    sql = Regex.Replace(sql, "SELECT", $"SELECT {topValue}", RegexOptions.IgnoreCase);
                }
            }

            if (string.IsNullOrWhiteSpace(sql) || sql.Length < 10) return compiled;

            var result = new SqlResult(sql, "@");
            foreach (var b in compiled.NamedBindings) result.NamedBindings[b.Key] = b.Value;

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogWarning("ApplySqlServerFixes falló: {0}. Retornando SQL original.", ex.Message);
            return compiled;
        }
    }
}