using Microsoft.Extensions.Logging;
using StockReservation.Domain;

namespace StockReservation.Application;

public sealed class ReservationService( IReservationRepository repository, ICurrentUser currentUser, IUnitOfWork unitOfWork, IClock clock, IApplicationCache cache,
    ILogger<ReservationService> logger) : IReservationService
{
    public async Task<ReservationResult> ReserveAsync( long purchaseOrderLineId, decimal quantity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting stock reservation for PO line {PurchaseOrderLineId}, quantity {Quantity}, user {UserId}", purchaseOrderLineId, quantity, currentUser.UserId);
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock order is deliberate: stock first, then PO line.
            // Reserve and release use the same order to minimise deadlock risk.
            var lineReference = await repository.GetLineAsync(purchaseOrderLineId, cancellationToken)
                ?? throw new NotFoundDomainException("Purchase order line was not found.");

            if (lineReference.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new DomainException("Only approved purchase orders can be reserved.");

            var stock = await repository.GetStockForUpdateAsync(lineReference.PurchaseOrder.WarehouseId, lineReference.InventoryItemId, cancellationToken)
                ?? throw new NotFoundDomainException("Warehouse stock was not found.");

            var line = await repository.GetLineForUpdateAsync(purchaseOrderLineId, cancellationToken)
                ?? throw new NotFoundDomainException("Purchase order line was not found.");

            if (line.PurchaseOrder.Status != PurchaseOrderStatus.Approved)
                throw new DomainException("Only approved purchase orders can be reserved.");

            if (line.PurchaseOrder.WarehouseId != stock.WarehouseId || line.InventoryItemId != stock.InventoryItemId)
                throw new DomainException("Purchase order line and warehouse stock are inconsistent.");

            QuantityRules.Validate(line.InventoryItem, quantity);

            if (quantity > line.OutstandingQuantity)
                throw new DomainException( $"Quantity exceeds PO outstanding quantity. Outstanding: {line.OutstandingQuantity:0.###}.");

            var available = await repository.GetAvailableStockAsync(stock.Id, cancellationToken);
            if (quantity > available)
                throw new DomainException($"Insufficient available stock. Available: {available:0.###}.");

            var now = clock.UtcNow;
            var reservation = new StockReservationEntity(line.Id, stock.Id, quantity, line.InventoryItem.StandardCost, now, currentUser.UserId);
            line.Reserve(quantity);
            repository.AddReservation(reservation);
            // Save first so PostgreSQL generates the reservation identity.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var availableAfter = available - quantity;
            repository.AddAudit(new AuditLogEntry(
                now,
                currentUser.UserId,
                currentUser.UserName,
                ReservationAction.Reserved,
                line.InventoryItemId,
                stock.WarehouseId,
                quantity,
                availableAfter,
                reservation.Id));

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            InvalidateCaches(stock.WarehouseId);

            logger.LogInformation( "Stock reservation {ReservationId} created for PO line {PurchaseOrderLineId}. Quantity {Quantity}, available stock {AvailableStock}",
                reservation.Id, line.Id, quantity, availableAfter);

            return new ReservationResult(
                reservation.Id,
                line.Id,
                quantity,
                line.OutstandingQuantity,
                reservation.RemainingQuantity,
                availableAfter,
                reservation.UnitCostSnapshot);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning("Stock reservation failed for PO line {PurchaseOrderLineId}, quantity {Quantity}, user {UserId}", purchaseOrderLineId, quantity, currentUser.UserId);
            throw;
        }
    }

    public async Task<ReservationResult> ReleaseAsync(long reservationId, decimal quantity, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting reservation release {ReservationId}, quantity {Quantity}, user {UserId}", reservationId, quantity, currentUser.UserId);
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var reservationReference = await repository.GetReservationAsync(reservationId, cancellationToken)
                ?? throw new NotFoundDomainException("Reservation was not found.");

            var stock = await repository.GetStockForUpdateAsync(
                    reservationReference.WarehouseStock.WarehouseId,
                    reservationReference.WarehouseStock.InventoryItemId,
                    cancellationToken)
                ?? throw new NotFoundDomainException("Warehouse stock was not found.");

            var line = await repository.GetLineForUpdateAsync(reservationReference.PurchaseOrderLineId, cancellationToken)
                ?? throw new NotFoundDomainException("Purchase order line was not found.");

            var reservation = await repository.GetReservationForUpdateAsync(reservationId, cancellationToken)
                ?? throw new NotFoundDomainException("Reservation was not found.");

            if (reservation.WarehouseStockId != stock.Id || reservation.PurchaseOrderLineId != line.Id)
                throw new DomainException("Reservation relationships are inconsistent.");

            QuantityRules.Validate(line.InventoryItem, quantity);
            reservation.Release(quantity);
            line.Release(quantity);

            var available = await repository.GetAvailableStockAsync(stock.Id, cancellationToken);
            var availableAfter = available + quantity;

            repository.AddAudit(new AuditLogEntry(
                clock.UtcNow,
                currentUser.UserId,
                currentUser.UserName,
                ReservationAction.Released,
                stock.InventoryItemId,
                stock.WarehouseId,
                quantity,
                availableAfter,
                reservation.Id));

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            InvalidateCaches(stock.WarehouseId);

            logger.LogInformation("Reservation {ReservationId} released. Quantity {Quantity}, available stock {AvailableStock}", reservationId, quantity, availableAfter);

            return new ReservationResult(
                reservation.Id,
                reservation.PurchaseOrderLineId,
                -quantity,
                line.OutstandingQuantity,
                reservation.RemainingQuantity,
                availableAfter,
                reservation.UnitCostSnapshot);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning( "Reservation release failed for {ReservationId}, quantity {Quantity}, user {UserId}", reservationId, quantity, currentUser.UserId);
            throw;
        }
    }

    private void InvalidateCaches(long warehouseId)
    {
        cache.Remove(CacheKeys.PurchaseOrders(warehouseId));
        cache.Remove(CacheKeys.FinanceCommittedStockValue);
    }
}
