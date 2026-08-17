using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockReservation.Domain;

namespace StockReservation.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("CK_inventory_items_standard_cost_non_negative", "\"StandardCost\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StandardCost).HasPrecision(18, 4);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("warehouse_stocks", table =>
        {
            table.HasCheckConstraint("CK_warehouse_stocks_on_hand_non_negative", "\"OnHandQuantity\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WarehouseId, x.InventoryItemId }).IsUnique();
        builder.Property(x => x.OnHandQuantity).HasPrecision(18, 3);
        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Number).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines", table =>
        {
            table.HasCheckConstraint("CK_purchase_order_lines_quantity_valid", "\"QuantityOrdered\" > 0 AND \"QuantityReserved\" >= 0 AND \"QuantityReserved\" <= \"QuantityOrdered\"");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityOrdered).HasPrecision(18, 3);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 3);
        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservationEntity>
{
    public void Configure(EntityTypeBuilder<StockReservationEntity> builder)
    {
        builder.ToTable("stock_reservations", table =>
        {
            table.HasCheckConstraint("CK_stock_reservations_quantity_valid", "\"OriginalQuantity\" > 0 AND \"ReleasedQuantity\" >= 0 AND \"ReleasedQuantity\" <= \"OriginalQuantity\"");
            table.HasCheckConstraint("CK_stock_reservations_cost_non_negative", "\"UnitCostSnapshot\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ReleasedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(18, 4);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.WarehouseStockId);
        builder.HasIndex(x => x.PurchaseOrderLineId);
        builder.HasOne(x => x.PurchaseOrderLine)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.WarehouseStock)
            .WithMany()
            .HasForeignKey(x => x.WarehouseStockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.AvailableStockAfter).HasPrecision(18, 3);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.WarehouseId, x.InventoryItemId, x.Timestamp });
    }
}
