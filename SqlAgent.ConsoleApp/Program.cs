using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAgent.Domain.Interfaces;
using SqlAgent.Domain.Models;
using SqlAgent.Infrastructure.Data;
using SqlAgent.Infrastructure.Engine;
using SqlAgent.Infrastructure.Repositories;
using SqlAgent.Infrastructure.Services;

// ====================================================
//   AGENTE SQL v3.0: TEST DE ENRUTAMIENTO (FASE 2)
// ====================================================

const string northwindCn = "Server=localhost,1433;Database=Northwind;User Id=sa;Password=Chopsuey00;TrustServerCertificate=True;";
//"Server=MEXIT1041\\MEXIT1041;Database=Northwind;" +
//"Integrated Security=True;TrustServerCertificate=True;";
Console.WriteLine("Iniciando Agente SQL v3.0...\n");

// 1. Configuración de Servicios (Inyección de Dependencias)
var services = new ServiceCollection();
services.AddDbContext<CatalogDbContext>(opt => opt.UseInMemoryDatabase("CatalogDb"));
services.AddScoped<ICatalogRepository, CatalogRepository>();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddScoped<QueryExecutor>(_ => new QueryExecutor(northwindCn));

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

// 2. Preparación del Catálogo (Semilla de Datos)
var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
await CatalogSeeder.SeedAsync(db);

var version = await db.Versions.FirstAsync();
var repo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

// El ProfileService implementa ISchemaProvider e IRelationshipResolver
var profile = await ProfileService.LoadAsync(version.Id, repo);

// 3. Inicialización del Pipeline (Fase 2: Incluye JoinPlanner internamente)
var logger = scope.ServiceProvider.GetRequiredService<ILogger<SqlAgentPipeline>>();
var executor = scope.ServiceProvider.GetRequiredService<QueryExecutor>();
var pipeline = new SqlAgentPipeline(profile, profile, executor, logger);

// 4. TEST DE INTELIGENCIA: Pedimos campos de dos tablas SIN declarar el JOIN
var intent = new QueryModel(
    EntityLogical: "Order",
    FieldsLogical: new List<string> { "OrderDate", "OrderDetail.Quantity" },
    JoinsLogical: null,  // <--- El motor lo resolverá solo
    Filters: null,
    GroupByLogical: null,
    OrderBy: new FieldOrderBy("Order", "OrderDate"),
    OrderDescending: true,
    Top: 5,
    MetricLogical: "Revenue"
);

try
{
    Console.WriteLine("Ejecutando Pipeline (Planner -> Guard -> Binder)...");
    var result = await pipeline.RunAsync(intent);

    Console.WriteLine("\n--- RESULTADOS ---");
    foreach (var row in result.Data)
    {
        var fecha = row.TryGetValue("OrderDate", out var d) ? d?.ToString() : "N/A";
        var cant = row.TryGetValue("Quantity", out var q) ? q?.ToString() : "N/A";
        var rev = row.TryGetValue("Revenue", out var r) ? r?.ToString() : "0";
        Console.WriteLine($"Fecha: {fecha} | Cantidad: {cant} | Rev: {rev}");
    }

    Console.WriteLine("\n--- SQL GENERADO ---");
    Console.WriteLine(result.GeneratedSql);

    if (result.GeneratedSql.Contains("JOIN", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine("\n[EXITO] Fase 2: El JoinPlanner resolvió el JOIN automáticamente.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERROR] {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"Detalle: {ex.InnerException.Message}");
}

Console.WriteLine("\nPresiona una tecla para salir...");
Console.ReadKey();