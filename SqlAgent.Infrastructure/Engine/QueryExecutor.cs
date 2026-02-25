using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using SqlKata;

namespace SqlAgent.Infrastructure.Engine;

/// <summary>
/// Responsable de ejecutar la consulta física generada por QueryBinder contra la base de datos real del cliente.
/// </summary>
public class QueryExecutor
{
    private readonly string _connectionString;

    // En un entorno real SaaS, este ConnectionString vendría del 'DataSource' del Perfil activo.
    public QueryExecutor(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Ejecuta el SqlResult y devuelve una lista de diccionarios (filas dinámicas).
    /// </summary>
    public async Task<IEnumerable<IDictionary<string, object>>> ExecuteAsync(SqlResult sqlResult)
    {
        using IDbConnection db = new SqlConnection(_connectionString);

        // Dapper mapea dinámicamente el resultado a un ExpandoObject (que implementa IDictionary)
        var result = await db.QueryAsync(sqlResult.Sql, sqlResult.NamedBindings);

        var rows = new List<IDictionary<string, object>>();
        foreach (var row in result)
        {
            rows.Add((IDictionary<string, object>)row);
        }

        return rows;
    }

    /// <summary>
    /// Método auxiliar para testear la conexión al vuelo.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Solución: Declarar explícitamente como SqlConnection
            using SqlConnection db = new SqlConnection(_connectionString);
            await db.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}