using StockReservation.Application;
using StockReservation.Domain;
using System;
using Xunit;

namespace StockReservation.UnitTests;

public sealed class DomainTests
{
    [Fact]
    public void Reserve_Cannot_Exceed_Outstanding()
    {
        var line = new PurchaseOrderLine(1, 1, 100m);

        line.Reserve(40m);

        Assert.Throws<DomainException>(() => line.Reserve(61m));
        Assert.Equal(60m, line.OutstandingQuantity);
    }

    [Fact]
    public void Release_Cannot_Exceed_Remaining()
    {
        var reservation = new StockReservationEntity(
            1, 1, 25m, 10m, DateTimeOffset.UtcNow, "u");

        reservation.Release(10.5m);

        Assert.Equal(14.5m, reservation.RemainingQuantity);
        Assert.Throws<DomainException>(() => reservation.Release(14.501m));
    }

    [Fact]
    public void Cost_Snapshot_Does_Not_Change()
    {
        var reservation = new StockReservationEntity(
            1, 1, 40m, 10m, DateTimeOffset.UtcNow, "u");

        Assert.Equal(10m, reservation.UnitCostSnapshot);
    }

    [Fact]
    public void Unit_Tracked_Items_Require_Whole_Quantities()
    {
        var item = new InventoryItem("BOX", "Box", 1, 3.20m, TrackingType.Unit);

        Assert.Throws<DomainException>(() => QuantityRules.Validate(item, 1.5m));
        QuantityRules.Validate(item, 2m);
    }

    [Fact]
    public void Weight_Tracked_Items_Support_Three_Decimal_Places()
    {
        var item = new InventoryItem("RICE", "Rice", 1, 22.50m, TrackingType.Weight);

        QuantityRules.Validate(item, 10.125m);
        Assert.Throws<DomainException>(() => QuantityRules.Validate(item, 10.1255m));
    }

    [Fact]
    public void Audit_Log_Stores_The_Resulting_Available_Balance()
    {
        var audit = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            "user-1",
            "Warehouse Operator",
            ReservationAction.Reserved,
            10,
            20,
            25.500m,
            74.500m,
            99);

        Assert.Equal(25.500m, audit.Quantity);
        Assert.Equal(74.500m, audit.AvailableStockAfter);
        Assert.Equal(99, audit.ReservationId);
    }
}
