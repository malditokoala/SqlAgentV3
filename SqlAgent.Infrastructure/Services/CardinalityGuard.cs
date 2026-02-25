using System;
using System.Collections.Generic;
using System.Linq;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Domain.Services;

/// <summary>
/// Previene la multiplicación silenciosa de filas en consultas que involucran JOINs y agrupaciones (GROUP BY).
/// Implementación de la directiva "Business Correctness" de la Arquitectura v3.0.
/// </summary>
public class CardinalityGuard
{
    private readonly ISchemaProvider _schema;

    public CardinalityGuard(ISchemaProvider schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Analiza y muta el QueryModel para asegurar que las agrupaciones sean seguras.
    /// </summary>
    public void EnsureSafeGrouping(QueryModel query)
    {
        // 1. Si no hay agrupaciones solicitadas, no hay riesgo de sumarizados incorrectos.
        if (query.GroupByLogical == null || !query.GroupByLogical.Any())
            return;

        // 2. Si no hay JOINs, la cardinalidad de la tabla base se mantiene intacta.
        if (query.JoinsLogical == null || !query.JoinsLogical.Any())
            return;

        // 3. Determinar qué entidades están involucradas en la consulta
        var involvedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { query.EntityLogical };
        foreach (var join in query.JoinsLogical)
        {
            involvedEntities.Add(join.ToEntityLogical);
        }

        // 4. Buscar si alguna de las entidades involucradas tiene definido un 'DefaultGrainField'
        //    (la llave primaria lógica que define la granularidad de esa entidad).
        var requiredGrainFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in involvedEntities)
        {
            var grainField = _schema.GetDefaultGrainField(entity);
            if (!string.IsNullOrEmpty(grainField))
            {
                requiredGrainFields.Add(grainField);
            }
        }

        // 5. Inyectar forzosamente los DefaultGrainFields en el GROUP BY y en el SELECT
        //    si no estaban presentes.
        foreach (var grainField in requiredGrainFields)
        {
            // El formato esperado es "EntityLogical.FieldLogical"
            if (!query.GroupByLogical.Contains(grainField, StringComparer.OrdinalIgnoreCase))
            {
                query.GroupByLogical.Add(grainField);

                // Si lo agrupamos, también debemos seleccionarlo para que la consulta sea válida en SQL
                if (!query.FieldsLogical.Contains(grainField, StringComparer.OrdinalIgnoreCase))
                {
                    query.FieldsLogical.Add(grainField);
                }
            }
        }
    }
}