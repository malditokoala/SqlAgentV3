using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqlAgent.Domain.Models;
using SqlAgent.Domain.Services;
using SqlAgent.Domain.Interfaces;

namespace SqlAgent.Infrastructure.Engine;

/// <summary>
/// El resultado final del pipeline, incluye los datos y metadatos de la ejecución.
/// </summary>
public record PipelineResult(
    IEnumerable<IDictionary<string, object>> Data,
    string GeneratedSql,
    Dictionary<string, object> Bindings,
    TimeSpan ExecutionTime
);

/// <summary>
/// Coordina el flujo completo: Validación -> Seguridad -> Generación SQL -> Ejecución.
/// </summary>
public class SqlAgentPipeline
{
    private readonly CardinalityGuard _guard;
    private readonly QueryBinder _binder;

    public SqlAgentPipeline(ISchemaProvider schemaProvider)
    {
        // Inicializamos los componentes del motor usando el proveedor de esquema (ProfileService)
        _guard = new CardinalityGuard(schemaProvider);
        _binder = new QueryBinder(schemaProvider);
    }

    /// <summary>
    /// Ejecuta el flujo completo a partir de la intención generada por el LLM.
    /// </summary>
    /// <param name="intent">El JSON parseado del LLM (QueryModel)</param>
    /// <param name="targetConnectionString">La cadena de conexión de la base de datos del cliente</param>
    public async Task<PipelineResult> RunAsync(QueryModel intent, string targetConnectionString)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        // 1. Reglas de Negocio (CardinalityGuard): 
        // Intercepta la consulta y asegura que no multiplicaremos filas por error en los JOINs.
        _guard.EnsureSafeGrouping(intent);

        // 2. Traducción (QueryBinder): 
        // Lógico -> Físico y generación de SQL parametrizado de forma segura usando SqlKata.
        var sqlResult = _binder.Bind(intent);

        // --- DEPURACIÓN: Ver el SQL antes de que Dapper intente ejecutarlo ---
        Console.WriteLine("\n[SQL GENERADO POR SQLKATA (Físico)]");
        Console.WriteLine(sqlResult.Sql);
        Console.WriteLine("----------------------------------\n");

        // 3. Ejecución Física (QueryExecutor): 
        // Enviar el SQL seguro a la base de datos real del cliente vía Dapper.
        var executor = new QueryExecutor(targetConnectionString);
        var data = await executor.ExecuteAsync(sqlResult);

        watch.Stop();

        // 4. Retorno: 
        // Devolver los datos reales junto con la auditoría del SQL generado y el tiempo que tomó.
        return new PipelineResult(
            Data: data,
            GeneratedSql: sqlResult.Sql,
            Bindings: sqlResult.NamedBindings,
            ExecutionTime: watch.Elapsed
        );
    }
}