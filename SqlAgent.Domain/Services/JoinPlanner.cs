using System;
using System.Collections.Generic;
using System.Linq;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Domain.Models;

namespace SqlAgent.Domain.Services;

/// <summary>
/// Implementación de la Fase 2: Inteligencia de Enrutamiento.
/// Calcula la ruta óptima de JOINs entre entidades para evitar que el LLM alucine relaciones.
/// </summary>
public class JoinPlanner
{
    private readonly IRelationshipResolver _resolver;

    public JoinPlanner(IRelationshipResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Analiza el QueryModel y autocompleta la lista de JoinsLogical basándose en el grafo del catálogo.
    /// </summary>
    public QueryModel Plan(QueryModel query)
    {
        // 1. Extraer todas las entidades necesarias de la consulta
        var requiredEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // De los campos seleccionados (Formato: "Entidad.Campo")
        foreach (var field in query.FieldsLogical.Where(f => f.Contains('.')))
            requiredEntities.Add(field.Split('.')[0]);

        // De los filtros
        if (query.Filters != null)
            foreach (var filter in query.Filters)
                requiredEntities.Add(filter.EntityLogical);

        // De las agrupaciones
        if (query.GroupByLogical != null)
            foreach (var group in query.GroupByLogical.Where(g => g.Contains('.')))
                requiredEntities.Add(group.Split('.')[0]);

        // La entidad base no necesita ser unida a sí misma
        requiredEntities.Remove(query.EntityLogical);

        if (!requiredEntities.Any()) return query;

        // 2. Construir el grafo de adyacencia desde el catálogo
        var edges = _resolver.GetAvailableEdges();
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in edges)
        {
            if (!graph.ContainsKey(edge.Source)) graph[edge.Source] = new List<string>();
            graph[edge.Source].Add(edge.Target);
        }

        // 3. Calcular la ruta para cada entidad requerida
        var finalJoins = new List<JoinModel>();
        var joinedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { query.EntityLogical };

        foreach (var target in requiredEntities)
        {
            if (joinedEntities.Contains(target)) continue;

            var path = FindPathBFS(graph, joinedEntities, target);

            if (path == null)
                throw new InvalidOperationException($"JoinPlanner: No existe una ruta válida en el catálogo para llegar a '{target}'.");

            // Convertir la ruta en modelos de Join
            for (int i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];

                if (!joinedEntities.Contains(to))
                {
                    finalJoins.Add(new JoinModel(from, to, "INNER"));
                    joinedEntities.Add(to);
                }
            }
        }

        // Devolvemos el modelo actualizado con la estrategia de JOINs calculada
        return query with { JoinsLogical = finalJoins };
    }

    private List<string>? FindPathBFS(Dictionary<string, List<string>> graph, IEnumerable<string> startNodes, string target)
    {
        var queue = new Queue<List<string>>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in startNodes)
        {
            queue.Enqueue(new List<string> { start });
            visited.Add(start);
        }

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var current = path.Last();

            if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
                return path;

            if (graph.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        var newPath = new List<string>(path) { neighbor };
                        queue.Enqueue(newPath);
                    }
                }
            }
        }

        return null;
    }
}