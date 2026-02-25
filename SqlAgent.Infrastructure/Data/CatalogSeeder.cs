using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SqlAgent.Domain.Entities;

namespace SqlAgent.Infrastructure.Data;

/// <summary>
/// Constantes para los estados de la versión del perfil.
/// </summary>
public static class VersionStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Archived = "Archived";
}

/// <summary>
/// Clase responsable de la carga inicial de metadatos en el catálogo.
/// Refactorizada para cumplir con la Arquitectura Profile-Driven v3.0.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(CatalogDbContext context)
    {
        // 1. Asegurar la creación de la base de datos de catálogo
        await context.Database.EnsureCreatedAsync();

        // 2. Evitar duplicación de datos si ya existen perfiles
        if (await context.Profiles.AnyAsync()) return;

        var profileId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var orderEntityId = Guid.NewGuid();
        var orderDetailEntityId = Guid.NewGuid();

        // --- PERFIL Y VERSIÓN ---
        // Se establece ActiveVersionId desde el inicio para evitar nulos en el arranque
        var profile = new Profile
        {
            Id = profileId,
            Name = "Northwind Demo",
            TenantId = "Llanero-1",
            ActiveVersionId = versionId
        };

        var version = new ProfileVersion
        {
            Id = versionId,
            ProfileId = profileId,
            VersionName = "v1.0",
            Status = VersionStatus.Published // Uso de constantes en lugar de strings mágicos
        };

        var dataSource = new DataSource
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            ConnectionStringName = "NorthwindDb",
            Engine = "SqlServer"
        };

        // --- ENTIDADES ---
        var orderEntity = new Entity
        {
            Id = orderEntityId,
            VersionId = versionId,
            LogicalName = "Order",
            PhysicalName = "dbo.Orders",
            Alias = "o",
            Category = "fact",
            DefaultGrainFields = "OrderId"
        };

        var detailEntity = new Entity
        {
            Id = orderDetailEntityId,
            VersionId = versionId,
            LogicalName = "OrderDetail",
            PhysicalName = "dbo.OrderDetails",
            Alias = "od",
            Category = "fact",
            DefaultGrainFields = "OrderId,ProductId" // Grain compuesto (Order + Product)
        };

        // --- CAMPOS ---
        var orderFields = new List<Field>
        {
            new() { Id = Guid.NewGuid(), EntityId = orderEntityId, LogicalName = "OrderId", PhysicalName = "OrderID", DataType = "int" },
            new() { Id = Guid.NewGuid(), EntityId = orderEntityId, LogicalName = "OrderDate", PhysicalName = "OrderDate", DataType = "datetime" },
            new() { Id = Guid.NewGuid(), EntityId = orderEntityId, LogicalName = "CustomerId", PhysicalName = "CustomerID", DataType = "int" }, // Ajustado a int para consistencia
        };

        var detailFields = new List<Field>
        {
            new() { Id = Guid.NewGuid(), EntityId = orderDetailEntityId, LogicalName = "OrderId", PhysicalName = "OrderID", DataType = "int" },
            new() { Id = Guid.NewGuid(), EntityId = orderDetailEntityId, LogicalName = "ProductId", PhysicalName = "ProductID", DataType = "int" }, // Campo necesario para el grano
            new() { Id = Guid.NewGuid(), EntityId = orderDetailEntityId, LogicalName = "UnitPrice", PhysicalName = "UnitPrice", DataType = "decimal" },
            new() { Id = Guid.NewGuid(), EntityId = orderDetailEntityId, LogicalName = "Quantity", PhysicalName = "Quantity", DataType = "int" },
        };

        // --- RELACIONES ---
        var orderToDetail = new Relationship
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            FromEntityLogical = "Order",
            FromFieldLogical = "OrderId",
            ToEntityLogical = "OrderDetail",
            ToFieldLogical = "OrderId",
            JoinType = "INNER",
            IsApproved = true
        };

        // Guardado jerárquico de la configuración
        context.Profiles.Add(profile);
        context.Versions.Add(version);
        context.DataSources.Add(dataSource);
        context.Entities.AddRange(orderEntity, detailEntity);
        context.Fields.AddRange(orderFields);
        context.Fields.AddRange(detailFields);
        context.Relationships.Add(orderToDetail);

        await context.SaveChangesAsync();
    }
}