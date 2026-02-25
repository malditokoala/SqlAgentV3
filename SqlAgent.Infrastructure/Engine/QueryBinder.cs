using System;
using SqlKata;
using SqlKata.Compilers;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Engine;

/// <summary>
/// La única clase autorizada para traducir LogicalName a PhysicalName usando el SchemaProvider.
/// </summary>
public class QueryBinder
{
    private readonly ISchemaProvider _schema;
    private readonly SqlServerCompiler _compiler;

    public QueryBinder(ISchemaProvider schema)
    {
        _schema = schema;
        _compiler = new SqlServerCompiler();
    }

    /// <summary>
    /// Convierte la intención del LLM (QueryModel) en una consulta SQL ejecutable.
    /// </summary>
    public SqlResult Bind(QueryModel intent)
    {
        // 1. Resolver tabla principal
        var mainTablePhysical = _schema.ResolvePhysicalTable(intent.EntityLogical);
        var query = new Query(mainTablePhysical);

        // 2. Resolver SELECT
        if (intent.FieldsLogical != null && intent.FieldsLogical.Count > 0)
        {
            foreach (var fieldLogical in intent.FieldsLogical)
            {
                var physicalField = _schema.ResolvePhysicalField(intent.EntityLogical, fieldLogical);
                query.Select($"{mainTablePhysical}.{physicalField} AS {fieldLogical}");
            }
        }
        else
        {
            query.Select($"{mainTablePhysical}.*");
        }

        // 3. Resolver JOINs
        if (intent.JoinsLogical != null)
        {
            foreach (var join in intent.JoinsLogical)
            {
                // En la Fase 2, aquí es donde inyectamos la lógica de RelationshipResolver
                // Por ahora, asumimos que el LLM nos dice qué unir, pero nosotros 
                // traducimos los nombres de las tablas.
                var targetTablePhysical = _schema.ResolvePhysicalTable(join.ToEntityLogical);

                // NOTA: Para un JOIN real dinámico, necesitamos leer "JoinCondition" de la tabla Relationships.
                // Esta es una versión simplificada para la primera iteración del pipeline.
                query.Join(targetTablePhysical, j => j.WhereRaw("1=1")); // TODO: Implementar RelationshipResolver
            }
        }

        // 4. Resolver WHERE (Filtros)
        if (intent.Filters != null)
        {
            foreach (var filter in intent.Filters)
            {
                var physicalField = _schema.ResolvePhysicalField(filter.EntityLogical, filter.FieldLogical);
                var physicalTable = _schema.ResolvePhysicalTable(filter.EntityLogical);

                // SqlKata maneja automáticamente la parametrización para evitar SQL Injection
                query.Where($"{physicalTable}.{physicalField}", filter.Operator, filter.Value);
            }
        }

        // 5. Compilar a SQL string y parámetros (Dapper-ready)
        return _compiler.Compile(query);
    }
}