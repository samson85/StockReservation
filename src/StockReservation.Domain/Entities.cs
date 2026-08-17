namespace StockReservation.Domain;

public abstract class Entity
{
    public long Id { get; protected set; }
}

public sealed class Warehouse : Entity
{
    public string Name { get; private set; } = null!;
    public Warehouse(string name) => Name = name;
}

public sealed class Category : Entity
{
    public string Name { get; private set; } = null!;
    public Category(string name) => Name = name;
}

public sealed class InventoryItem : Entity
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public long CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public decimal StandardCost { get; private set; }
    public TrackingType TrackingType { get; private set; }

    public InventoryItem(string sku, string name, long categoryId, decimal standardCost, TrackingType trackingType)
    {
        if (standardCost < 0) throw new DomainException("Standard cost cannot be negative.");
        Sku = sku; Name = name; CategoryId = categoryId; StandardCost = standardCost; TrackingType = trackingType;
    }

    public void ChangeStandardCost(decimal cost)
    {
        if (cost < 0) throw new DomainException("Standard cost cannot be negative.");
        StandardCost = cost;
    }
}

public sealed class WarehouseStock : Entity
{
    public long WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public long InventoryItemId { get; private set; }
    public InventoryItem InventoryItem { get; private set; } = null!;
    public decimal OnHandQuantity { get; private set; }

    public WarehouseStock(long warehouseId, long inventoryItemId, decimal onHandQuantity)
    {
        if (onHandQuantity < 0) throw new DomainException("On-hand quantity cannot be negative.");
        WarehouseId = warehouseId; InventoryItemId = inventoryItemId; OnHandQuantity = onHandQuantity;
    }

    public void SetOnHand(decimal quantity)
    {
        if (quantity < 0) throw new DomainException("On-hand quantity cannot be negative.");
        OnHandQuantity = quantity;
    }
}

public sealed class PurchaseOrder : Entity
{
    public string Number { get; private set; } = null!;
    public long WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public ICollection<PurchaseOrderLine> Lines { get; private set; } = new List<PurchaseOrderLine>();

    public PurchaseOrder(string number, long warehouseId, PurchaseOrderStatus status = PurchaseOrderStatus.Draft)
    {
        Number = number; WarehouseId = warehouseId; Status = status;
    }

    public void Approve(DateTimeOffset at)
    {
        Status = PurchaseOrderStatus.Approved;
        ApprovedAt = at;
    }
}

public sealed class PurchaseOrderLine : Entity
{
    public long PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private set; } = null!;
    public long InventoryItemId { get; private set; }
    public InventoryItem InventoryItem { get; private set; } = null!;
    public decimal QuantityOrdered { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public ICollection<StockReservationEntity> Reservations { get; private set; } = new List<StockReservationEntity>();

    public PurchaseOrderLine(long purchaseOrderId, long inventoryItemId, decimal quantityOrdered)
    {
        if (quantityOrdered <= 0) throw new DomainException("Ordered quantity must be greater than zero.");
        PurchaseOrderId = purchaseOrderId; InventoryItemId = inventoryItemId; QuantityOrdered = quantityOrdered;
    }

    public decimal OutstandingQuantity => QuantityOrdered - QuantityReserved;

    public void Reserve(decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Reservation quantity must be greater than zero.");
        if (quantity > OutstandingQuantity) throw new DomainException("Reservation exceeds PO line outstanding quantity.");
        QuantityReserved += quantity;
    }

    public void Release(decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Release quantity must be greater than zero.");
        if (quantity > QuantityReserved) throw new DomainException("Release exceeds the PO line reserved quantity.");
        QuantityReserved -= quantity;
    }
}

public sealed class StockReservationEntity : Entity
{
    public long PurchaseOrderLineId { get; private set; }
    public PurchaseOrderLine PurchaseOrderLine { get; private set; } = null!;

    public long WarehouseStockId { get; private set; }
    public WarehouseStock WarehouseStock { get; private set; } = null!;

    public decimal OriginalQuantity { get; private set; }
    public decimal ReleasedQuantity { get; private set; }
    public decimal UnitCostSnapshot { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = null!;

    public decimal RemainingQuantity =>
        OriginalQuantity - ReleasedQuantity;

    // EF Core constructor
    private StockReservationEntity()
    {
    }

    // Domain constructor
    public StockReservationEntity(
        long lineId,
        long stockId,
        decimal quantity,
        decimal costSnapshot,
        DateTimeOffset createdAt,
        string userId)
    {
        if (quantity <= 0)
            throw new DomainException(
                "Reservation quantity must be greater than zero.");

        if (costSnapshot < 0)
            throw new DomainException(
                "Cost snapshot cannot be negative.");

        PurchaseOrderLineId = lineId;
        WarehouseStockId = stockId;
        OriginalQuantity = quantity;
        UnitCostSnapshot = costSnapshot;
        CreatedAt = createdAt;
        CreatedByUserId = userId;
    }

    public void Release(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException(
                "Release quantity must be greater than zero.");

        if (quantity > RemainingQuantity)
            throw new DomainException(
                "Release exceeds remaining reservation quantity.");

        ReleasedQuantity += quantity;
    }
}

public sealed class AuditLogEntry : Entity
{
    public DateTimeOffset Timestamp { get; private set; }
    public string UserId { get; private set; } = null!;
    public string UserName { get; private set; } = null!;
    public ReservationAction Action { get; private set; }
    public long InventoryItemId { get; private set; }
    public long WarehouseId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal AvailableStockAfter { get; private set; }
    public long? ReservationId { get; private set; }

    private AuditLogEntry() { }

    public AuditLogEntry(DateTimeOffset timestamp, string userId, string userName, ReservationAction action,
        long itemId, long warehouseId, decimal quantity, decimal availableAfter, long? reservationId)
    {
        Timestamp = timestamp; UserId = userId; UserName = userName; Action = action;
        InventoryItemId = itemId; WarehouseId = warehouseId; Quantity = quantity;
        AvailableStockAfter = availableAfter; ReservationId = reservationId;
    }
}
