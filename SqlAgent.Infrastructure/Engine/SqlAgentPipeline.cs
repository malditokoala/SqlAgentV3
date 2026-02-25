using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Services;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Engine;

public record PipelineResult(
    IEnumerable<IDictionary<string, object>> Data,
    string GeneratedSql,
    Dictionary<string, object> Bindings,
    TimeSpan ExecutionTime
);

public class SqlAgentPipeline
{
    private readonly CardinalityGuard _guard;
    private readonly QueryBinder _binder;
    private readonly QueryExecutor _executor;
    private readonly ILogger<SqlAgentPipeline> _logger;

    public SqlAgentPipeline(
        ISchemaProvider schemaProvider,
        IRelationshipResolver relationshipResolver,
        QueryExecutor executor,
        ILogger<SqlAgentPipeline> logger)
    {
        _guard = new CardinalityGuard(schemaProvider);
        _binder = new QueryBinder(schemaProvider, relationshipResolver);
        _executor = executor;
        _logger = logger;
    }

    public async Task<PipelineResult> RunAsync(QueryModel intent)
    {
        var watch = Stopwatch.StartNew();

        // 1. Validación de cardinalidad — devuelve un QueryModel nuevo, no muta el original
        var safeIntent = _guard.EnsureSafeGrouping(intent);

        // 2. Traducción lógico -> físico
        var sqlResult = _binder.Bind(safeIntent);

        _logger.LogDebug("SQL generado: {Sql}", sqlResult.Sql);

        // 3. Ejecución
        var data = await _executor.ExecuteAsync(sqlResult);

        watch.Stop();

        return new PipelineResult(
            Data: data,
            GeneratedSql: sqlResult.Sql,
            Bindings: sqlResult.NamedBindings,
            ExecutionTime: watch.Elapsed
        );
    }
}