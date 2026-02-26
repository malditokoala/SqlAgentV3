using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Domain.Models;
using SqlAgent.Infrastructure.Data;
using SqlAgent.Infrastructure.Engine;
using SqlAgent.Infrastructure.Repositories;
using SqlAgent.Infrastructure.Services;

namespace SqlAgent.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        const string northwindCn =
            "Server=MEXIT1041\\MEXIT1041;Database=Northwind;" +
            "Integrated Security=True;TrustServerCertificate=True;";

        Console.WriteLine("====================================================");
        Console.WriteLine("   AGENTE TEXT-TO-SQL: TEST FINAL SPRINT 1");
        Console.WriteLine("====================================================\n");

        // 1. DI
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase("CatalogDb"));
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // QueryExecutor necesita el connection string de la BD del cliente
        services.AddScoped<QueryExecutor>(_ => new QueryExecutor(northwindCn));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // 2. Seed del catálogo
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await CatalogSeeder.SeedAsync(db);

        // 3. Cargar ProfileService (implementa ISchemaProvider + IRelationshipResolver)
        var version = await db.Versions.FirstAsync();
        var repo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var schema = await ProfileService.LoadAsync(version.Id, repo);

        // 4. Construir pipeline con todas las dependencias
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SqlAgentPipeline>>();
        var executor = scope.ServiceProvider.GetRequiredService<QueryExecutor>();
        var pipeline = new SqlAgentPipeline(schema, schema, executor, logger);

        // 5. QueryModel correcto para el test
        var intent = new QueryModel(
            EntityLogical: "Order",
            FieldsLogical: new List<string> { "OrderDate" },
            Filters: null,
            // CORREGIDO: JoinModel se instancia solo con el destino (ToEntityLogical) según tu dominio local
            JoinsLogical: new List<JoinModel> { new JoinModel("OrderDetail") },
            GroupByLogical: new List<string> { "OrderDate" },
            OrderBy: null,
            OrderDescending: false,
            Top: 10,
            MetricLogical: "Revenue"  // necesario para activar CardinalityGuard
        );

        try
        {
            Console.WriteLine("Ejecutando consulta con protección de cardinalidad...\n");
            var result = await pipeline.RunAsync(intent);

            Console.WriteLine("--- DATOS ---");
            foreach (var row in result.Data)
            {
                var orderDate = row.TryGetValue("OrderDate", out var od) ? od?.ToString() ?? "?" : "?";
                var orderId = row.TryGetValue("OrderId", out var oi) ? oi?.ToString() ?? "No inyectado" : "No inyectado";
                Console.WriteLine($"Fecha: {orderDate} | Grain (OrderId): {orderId}");
            }

            Console.WriteLine($"\n[SQL] {result.GeneratedSql}");
            Console.WriteLine($"[TIME] {result.ExecutionTime.TotalMilliseconds}ms");

            Console.WriteLine(result.GeneratedSql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase)
                ? "\n[OK] CardinalityGuard inyectó el grain correctamente."
                : "\n[WARN] No se detectó GROUP BY en el SQL generado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine("\nPresiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}