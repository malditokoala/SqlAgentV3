using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Domain.Models;
using SqlAgent.Infrastructure.Data;
using SqlAgent.Infrastructure.Engine;
using SqlAgent.Infrastructure.Repositories;
using SqlAgent.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlAgent.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        // CONFIGURACIÓN DE CONEXIÓN
        // Usamos la instancia local MEXIT1041 reportada en los logs.
        string northwindCn = "Server=MEXIT1041\\MEXIT1041;Database=Northwind;Integrated Security=True;TrustServerCertificate=True;";

        Console.WriteLine("====================================================");
        Console.WriteLine("   AGENTE TEXT-TO-SQL: TEST FINAL SPRINT 1");
        Console.WriteLine("====================================================\n");

        // 1. Setup de Infraestructura para el Catálogo
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase("CatalogDb"));
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // SEEDING: Es vital que en CatalogSeeder.cs el PhysicalName sea "dbo.OrderDetails" (sin corchetes).
        await CatalogSeeder.SeedAsync(db);

        // 2. Cargar motor
        var version = await db.Versions.FirstAsync();
        var repo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        // Cargamos el esquema. Se ha corregido ProfileService para cargar DefaultGrainFields.
        var schema = await ProfileService.LoadAsync(version.Id, repo);
        var pipeline = new SqlAgentPipeline(schema);

        // 3. CASO DE PRUEBA: "Ventas por fecha"
        // Este modelo simula una petición que requiere JOIN y provoca una agrupación.
        var intent = new QueryModel(
            EntityLogical: "Order",
            FieldsLogical: new List<string> { "OrderDate" },
            Filters: null,
            JoinsLogical: new List<JoinModel>
            {
                new JoinModel("Order", "OrderDetail", "INNER")
            },
            GroupByLogical: new List<string> { "OrderDate" },
            OrderBy: null,
            OrderDescending: false,
            Top: 10,
            MetricLogical: null
        );

        try
        {
            Console.WriteLine("Ejecutando consulta compleja con protección de cardinalidad...");
            var result = await pipeline.RunAsync(intent, northwindCn);

            Console.WriteLine("\n--- DATOS RECUPERADOS DESDE SQL SERVER ---");
            foreach (var row in result.Data)
            {
                // Verificamos si el CardinalityGuard inyectó el OrderId
                string orderIdStr = row.ContainsKey("OrderId") ? row["OrderId"].ToString() : "No Inyectado";
                Console.WriteLine($"Fecha: {row["OrderDate"]} | Grain ID (OrderId): {orderIdStr}");
            }

            Console.WriteLine($"\n[INFO] SQL Generado: {result.GeneratedSql}");
            Console.WriteLine($"[INFO] Tiempo de Ejecución: {result.ExecutionTime.TotalMilliseconds}ms");

            if (result.GeneratedSql.Contains("OrderID"))
            {
                Console.WriteLine("\n[EXITO] El motor protegió la integridad de la consulta inyectando el grano.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR DE EJECUCIÓN]: {ex.Message}");

            if (ex.Message.Contains("Invalid object name"))
            {
                Console.WriteLine("\n🚨 NOTA DE DEPURACIÓN:");
                Console.WriteLine("El SQL generado sigue teniendo corchetes anidados.");
                Console.WriteLine("Asegúrate de limpiar el campo PhysicalName en tu base de datos o en el Seeder.");
            }
        }

        Console.WriteLine("\nPresiona cualquier tecla para finalizar el Sprint 1...");
        Console.ReadKey();
    }
}