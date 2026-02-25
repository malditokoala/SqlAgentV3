using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Models;

// ── Tipos discriminados para manejar el ORDER BY con seguridad ──
public abstract record QueryOrderBy;

public record FieldOrderBy(string EntityLogical, string FieldLogical) : QueryOrderBy;
public record MetricOrderBy(string MetricLogical) : QueryOrderBy;

// ── Modelos auxiliares ──
public record FilterModel(string EntityLogical, string FieldLogical, string Operator, string Value);
public record JoinModel(string FromEntityLogical, string ToEntityLogical, string JoinType = "INNER");

// ── La orden de trabajo maestra (100% Nombres Lógicos) ──
public record QueryModel(
    string EntityLogical,
    List<string> FieldsLogical,
    List<FilterModel> Filters,
    List<JoinModel> JoinsLogical,
    List<string> GroupByLogical,
    QueryOrderBy? OrderBy,        // Usa el tipo discriminado
    bool OrderDescending,
    int? Top,
    string? MetricLogical
);