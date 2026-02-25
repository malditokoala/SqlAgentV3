// SqlAgent.Domain/Models/QueryModel.cs
namespace SqlAgent.Domain.Models;

public record JoinModel(string ToEntityLogical);

public record FilterModel(
    string EntityLogical,
    string FieldLogical,
    string Operator,
    object Value
);

// Tipo discriminado para ORDER BY — evita el bug de ordenar por métrica como si fuera campo
public abstract record QueryOrderBy;
public record FieldOrderBy(string EntityLogical, string FieldLogical) : QueryOrderBy;
public record MetricOrderBy(string MetricLogical) : QueryOrderBy;

public record QueryModel(
    string EntityLogical,
    List<string> FieldsLogical,
    List<JoinModel>? JoinsLogical,
    List<FilterModel>? Filters,
    List<string>? GroupByLogical,
    QueryOrderBy? OrderBy,
    bool OrderDescending,
    int? Top,
    string? MetricLogical
);