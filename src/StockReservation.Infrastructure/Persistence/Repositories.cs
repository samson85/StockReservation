using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockReservation.Application;
using StockReservation.Domain;

namespace StockReservation.Infrastructure.Persistence;

public sealed class ReservationRepository( AppDbContext db, ILogger<ReservationRepository> logger) : IReservationRepository
{
    public Task<PurchaseOrderLine?> GetLineAsync(long id, CancellationToken ct) =>
        db.PurchaseOrderLines
            .AsNoTracking()
            .Include(x => x.PurchaseOrder)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PurchaseOrderLine?> GetLineForUpdateAsync(long id, CancellationToken ct)
    {
        await LockPurchaseOrderLineAsync(id, ct);

        return await db.PurchaseOrderLines
            .Include(x => x.PurchaseOrder)
            .Include(x => x.InventoryItem)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<WarehouseStock?> GetStockForUpdateAsync( long warehouseId, long itemId,  CancellationToken ct)
    {
        await LockWarehouseStockAsync(warehouseId, itemId, ct);

        return await db.WarehouseStocks
            .SingleOrDefaultAsync(
                x => x.WarehouseId == warehouseId && x.InventoryItemId == itemId,
                ct);
    }

    public Task<StockReservationEntity?> GetReservationAsync(long id, CancellationToken ct) =>
        db.StockReservations
            .AsNoTracking()
            .Include(x => x.PurchaseOrderLine)
            .Include(x => x.WarehouseStock)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<StockReservationEntity?> GetReservationForUpdateAsync(long id, CancellationToken ct)
    {
        await LockReservationAsync(id, ct);

        return await db.StockReservations
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<decimal> GetAvailableStockAsync(long stockId, CancellationToken ct)
    {
        var onHand = await db.WarehouseStocks
            .Where(x => x.Id == stockId)
            .Select(x => (decimal?)x.OnHandQuantity)
            .SingleOrDefaultAsync(ct) ??
            throw new NotFoundDomainException("Warehouse stock was not found.");

        var reserved = await db.StockReservations
            .Where(x => x.WarehouseStockId == stockId)
            .SumAsync(x => x.OriginalQuantity - x.ReleasedQuantity, ct);

        return onHand - reserved;
    }

    public void AddReservation(StockReservationEntity reservation) => db.StockReservations.Add(reservation);

    public void AddAudit(AuditLogEntry entry) => db.AuditLogEntries.Add(entry);

    private async Task LockPurchaseOrderLineAsync(long id, CancellationToken ct)
    {
        logger.LogDebug("Acquiring row lock for purchase order line {PurchaseOrderLineId}", id);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT "Id" FROM purchase_order_lines WHERE "Id" = {id} FOR UPDATE""",
            ct);
    }

    private async Task LockWarehouseStockAsync(long warehouseId, long itemId, CancellationToken ct)
    {
        logger.LogDebug(
            "Acquiring row lock for warehouse stock {WarehouseId}/{InventoryItemId}",
            warehouseId, itemId);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT "Id"
            FROM warehouse_stocks
            WHERE "WarehouseId" = {warehouseId}
              AND "InventoryItemId" = {itemId}
            FOR UPDATE
            """,
            ct);
    }

    private async Task LockReservationAsync(long id, CancellationToken ct)
    {
        logger.LogDebug("Acquiring row lock for reservation {ReservationId}", id);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT "Id" FROM stock_reservations WHERE "Id" = {id} FOR UPDATE""",
            ct);
    }
}

public sealed class EfUnitOfWork( AppDbContext db, ILogger<EfUnitOfWork> logger) : IUnitOfWork
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction;

    public async Task BeginTransactionAsync(CancellationToken ct)
    {
        transaction = await db.Database.BeginTransactionAsync(ct);
        logger.LogDebug("Database transaction started");
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task CommitAsync(CancellationToken ct)
    {
        if (transaction is null)
            return;

        await transaction.CommitAsync(ct);
        await transaction.DisposeAsync();
        transaction = null;
        logger.LogDebug("Database transaction committed");
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (transaction is null)
            return;

        await transaction.RollbackAsync(ct);
        await transaction.DisposeAsync();
        transaction = null;
        logger.LogDebug("Database transaction rolled back");
    }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class DatabaseInitializer( AppDbContext db, ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        logger.LogInformation("Ensuring database schema is available");
        await db.Database.EnsureCreatedAsync(ct);
        logger.LogInformation("Database schema is ready");
    }
}
