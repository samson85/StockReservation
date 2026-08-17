using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockReservation.Application;
using StockReservation.Domain;
using StockReservation.Infrastructure.Persistence;

namespace StockReservation.Infrastructure.Queries;

public class PurchaseOrderQueries( AppDbContext db, ILogger<PurchaseOrderQueries> logger) : IPurchaseOrderQueries
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> GetOutstandingAsync( long warehouseId,  CancellationToken cancellationToken)
    {
        logger.LogDebug( "Querying outstanding purchase orders for warehouse {WarehouseId}", warehouseId);

        var orders = await GetOrdersAsync( warehouseId, cancellationToken);

        if (orders.Count == 0)
        {
            return [];
        }

        var itemIds = orders
            .SelectMany(order => order.Lines)
            .Select(line => line.InventoryItemId)
            .Distinct()
            .ToList();

        var onHandByItem = await GetOnHandByItemAsync( warehouseId, itemIds, cancellationToken);

        var reservedByItem = await GetReservedByItemAsync( warehouseId, itemIds, cancellationToken);

        return orders
            .Select(order => MapOrder(
                order,
                onHandByItem,
                reservedByItem))
            .ToList();
    }

    private async Task<List<PurchaseOrder>> GetOrdersAsync( long warehouseId, CancellationToken cancellationToken)
    {
        return await db.PurchaseOrders
            .AsNoTracking()
            .Where(order =>
                order.WarehouseId == warehouseId &&
                order.Status == PurchaseOrderStatus.Approved &&
                order.Lines.Any(line =>
                    line.QuantityReserved < line.QuantityOrdered))
            .Include(order => order.Warehouse)
            .Include(order => order.Lines)
                .ThenInclude(line => line.InventoryItem)
            .Include(order => order.Lines)
                .ThenInclude(line => line.Reservations)
            .OrderBy(order => order.Number)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<long, decimal>> GetOnHandByItemAsync( long warehouseId, IReadOnlyCollection<long> itemIds, CancellationToken cancellationToken)
    {
        return await db.WarehouseStocks
            .AsNoTracking()
            .Where(stock =>
                stock.WarehouseId == warehouseId &&
                itemIds.Contains(stock.InventoryItemId))
            .ToDictionaryAsync(
                stock => stock.InventoryItemId,
                stock => stock.OnHandQuantity,
                cancellationToken);
    }

    private async Task<Dictionary<long, decimal>> GetReservedByItemAsync( long warehouseId,IReadOnlyCollection<long> itemIds, CancellationToken cancellationToken)
    {
        return await db.StockReservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.WarehouseStock.WarehouseId == warehouseId &&
                itemIds.Contains(reservation.WarehouseStock.InventoryItemId))
            .GroupBy(reservation =>
                reservation.WarehouseStock.InventoryItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                ReservedQuantity = group.Sum(reservation =>
                    reservation.OriginalQuantity -
                    reservation.ReleasedQuantity)
            })
            .ToDictionaryAsync(
                result => result.ItemId,
                result => result.ReservedQuantity,
                cancellationToken);
    }

    private static PurchaseOrderDto MapOrder( PurchaseOrder order, IReadOnlyDictionary<long, decimal> onHandByItem, IReadOnlyDictionary<long, decimal> reservedByItem)
    {
        var lines = order.Lines
            .Where(HasOutstandingQuantity)
            .Select(line => MapLine(
                line,
                onHandByItem,
                reservedByItem))
            .ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.Number,
            order.WarehouseId,
            order.Warehouse.Name,
            lines);
    }

    private static PurchaseOrderLineDto MapLine( PurchaseOrderLine line, IReadOnlyDictionary<long, decimal> onHandByItem, IReadOnlyDictionary<long, decimal> reservedByItem)
    {
        var onHand = GetQuantity(
            onHandByItem,
            line.InventoryItemId);

        var reserved = GetQuantity(
            reservedByItem,
            line.InventoryItemId);

        var available = onHand - reserved;

        var reservations = line.Reservations
            .Where(reservation => reservation.RemainingQuantity > 0)
            .OrderByDescending(reservation => reservation.CreatedAt)
            .Select(reservation => new ReservationDto(
                reservation.Id,
                reservation.RemainingQuantity,
                reservation.UnitCostSnapshot,
                reservation.CreatedAt,
                reservation.CreatedByUserId))
            .ToList();

        return new PurchaseOrderLineDto(
            line.Id,
            line.InventoryItemId,
            line.InventoryItem.Sku,
            line.InventoryItem.Name,
            line.InventoryItem.TrackingType,
            line.QuantityOrdered,
            line.QuantityReserved,
            line.OutstandingQuantity,
            available,
            reservations);
    }

    private static bool HasOutstandingQuantity(PurchaseOrderLine line)
    {
        return line.QuantityReserved < line.QuantityOrdered;
    }

    private static decimal GetQuantity(
        IReadOnlyDictionary<long, decimal> quantities,
        long itemId)
    {
        return quantities.TryGetValue(itemId, out var quantity)
            ? quantity
            : 0m;
    }
}
