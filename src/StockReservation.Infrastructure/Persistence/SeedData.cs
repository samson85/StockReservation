using Microsoft.EntityFrameworkCore;
using StockReservation.Domain;

namespace StockReservation.Infrastructure.Persistence;

public static class SeedData
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Warehouse>().HasData(
            new { Id = 1L, Name = "Warehouse A" },
            new { Id = 2L, Name = "Warehouse B" });

        builder.Entity<Category>().HasData(
            new { Id = 1L, Name = "Bulk Goods" },
            new { Id = 2L, Name = "General" });

        builder.Entity<InventoryItem>().HasData(
            new { Id = 1L, Sku = "RICE-25", Name = "Rice 25kg", CategoryId = 1L, StandardCost = 22.50m, TrackingType = TrackingType.Weight },
            new { Id = 2L, Sku = "BOX-001", Name = "Shipping Box", CategoryId = 2L, StandardCost = 3.20m, TrackingType = TrackingType.Unit },
            new { Id = 3L, Sku = "BOLT-001", Name = "Steel Bolt", CategoryId = 2L, StandardCost = 0.75m, TrackingType = TrackingType.Unit });

        builder.Entity<WarehouseStock>().HasData(
            new { Id = 1L, WarehouseId = 1L, InventoryItemId = 1L, OnHandQuantity = 100m },
            new { Id = 2L, WarehouseId = 1L, InventoryItemId = 2L, OnHandQuantity = 250m },
            new { Id = 3L, WarehouseId = 1L, InventoryItemId = 3L, OnHandQuantity = 1000m },
            new { Id = 4L, WarehouseId = 2L, InventoryItemId = 1L, OnHandQuantity = 75m });

        builder.Entity<PurchaseOrder>().HasData(
            new { Id = 1L, Number = "PO-10001", WarehouseId = 1L, Status = PurchaseOrderStatus.Approved, ApprovedAt = DateTimeOffset.Parse("2026-08-15T00:00:00Z") },
            new { Id = 2L, Number = "PO-10002", WarehouseId = 1L, Status = PurchaseOrderStatus.Approved, ApprovedAt = DateTimeOffset.Parse("2026-08-15T00:00:00Z") },
            new { Id = 3L, Number = "PO-10003", WarehouseId = 2L, Status = PurchaseOrderStatus.Approved, ApprovedAt = DateTimeOffset.Parse("2026-08-15T00:00:00Z") });

        builder.Entity<PurchaseOrderLine>().HasData(
            new { Id = 1L, PurchaseOrderId = 1L, InventoryItemId = 1L, QuantityOrdered = 80m, QuantityReserved = 0m },
            new { Id = 2L, PurchaseOrderId = 1L, InventoryItemId = 2L, QuantityOrdered = 100m, QuantityReserved = 0m },
            new { Id = 3L, PurchaseOrderId = 2L, InventoryItemId = 3L, QuantityOrdered = 400m, QuantityReserved = 0m },
            new { Id = 4L, PurchaseOrderId = 3L, InventoryItemId = 1L, QuantityOrdered = 50m, QuantityReserved = 0m });
    }
}
