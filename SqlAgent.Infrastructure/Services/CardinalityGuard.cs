using System;
using System.Collections.Generic;
using System.Linq;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Domain.Exceptions;

namespace SqlAgent.Domain.Services;

public class CardinalityGuard
{
    private readonly ISchemaProvider _schema;

    public CardinalityGuard(ISchemaProvider schema)
    {
        _schema = schema;
    }

    public QueryModel EnsureSafeGrouping(QueryModel query)
    {
        bool hasAggregation = !string.IsNullOrEmpty(query.MetricLogical);
        bool hasJoins = query.JoinsLogical?.Any() == true;

        if (!hasAggregation || !hasJoins) return query;

        var involvedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { query.EntityLogical };

        foreach (var join in query.JoinsLogical!)
            involvedEntities.Add(join.ToEntityLogical);

        // HashSet elimina duplicados entre entidades que comparten campos de grain
        var requiredGrainFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in involvedEntities)
        {
            var grainRaw = _schema.GetDefaultGrainFields(entity);

            if (string.IsNullOrWhiteSpace(grainRaw))
                throw new CardinalityViolationException(
                    $"La entidad '{entity}' no tiene DefaultGrainFields definido. " +
                    $"No es posible generar un GROUP BY seguro para una consulta con JOINs y métricas. " +
                    $"Ve a Admin > Entities > {entity} > DefaultGrainFields. " +
                    $"Ejemplo: 'OrderId' o 'OrderId,ProductId'.");

            var fields = grainRaw.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Formato "EntityLogical.FieldLogical" para que QueryBinder sepa de donde resolver
            foreach (var f in fields)
                requiredGrainFields.Add($"{entity}.{f}");
        }

        var newGroupBy = new List<string>(query.GroupByLogical ?? []);
        var newFields = new List<string>(query.FieldsLogical ?? []); // <-- FIX: Lista para el SELECT

        foreach (var grainField in requiredGrainFields)
        {
            if (!newGroupBy.Contains(grainField, StringComparer.OrdinalIgnoreCase))
                newGroupBy.Add(grainField);

            // FIX: Inyectar en el SELECT (FieldsLogical) para que Dapper lo pueda leer
            if (!newFields.Contains(grainField, StringComparer.OrdinalIgnoreCase))
                newFields.Add(grainField);
        }

        // FIX: Retornamos el query mutado con AMBAS listas actualizadas
        return query with
        {
            GroupByLogical = newGroupBy,
            FieldsLogical = newFields
        };
    }
}