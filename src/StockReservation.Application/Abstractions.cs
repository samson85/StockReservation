using StockReservation.Domain;

namespace StockReservation.Application;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct);
}

public interface ICurrentUser
{
    string UserId { get; }
    string UserName { get; }
}

public interface IReservationService
{
    Task<ReservationResult> ReserveAsync(long purchaseOrderLineId, decimal quantity, CancellationToken ct);
    Task<ReservationResult> ReleaseAsync(long reservationId, decimal quantity, CancellationToken ct);
}

public interface IPurchaseOrderQueries
{
    Task<IReadOnlyList<PurchaseOrderDto>> GetOutstandingAsync(long warehouseId, CancellationToken ct);
}

public interface IFinanceQueries
{
    Task<IReadOnlyList<WarehouseCommittedValueDto>> GetCommittedValuesAsync(CancellationToken ct);
}

public interface IReservationRepository
{
    Task<PurchaseOrderLine?> GetLineAsync(long id, CancellationToken ct);
    Task<PurchaseOrderLine?> GetLineForUpdateAsync(long id, CancellationToken ct);
    Task<WarehouseStock?> GetStockForUpdateAsync(long warehouseId, long itemId, CancellationToken ct);
    Task<StockReservationEntity?> GetReservationAsync(long id, CancellationToken ct);
    Task<StockReservationEntity?> GetReservationForUpdateAsync(long id, CancellationToken ct);
    Task<decimal> GetAvailableStockAsync(long stockId, CancellationToken ct);
    void AddReservation(StockReservationEntity reservation);
    void AddAudit(AuditLogEntry entry);
}

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Application-facing cache abstraction. The implementation is infrastructure-specific.
/// </summary>
public interface IApplicationCache
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan lifetime);
    void Remove(string key);
}

public static class CacheKeys
{
    public static string PurchaseOrders(long warehouseId) => $"purchase-orders:{warehouseId}";
    public const string FinanceCommittedStockValue = "finance:committed-stock-value";
}

public sealed record PurchaseOrderDto(
    long Id,
    string Number,
    long WarehouseId,
    string WarehouseName,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderLineDto(
    long Id,
    long ItemId,
    string Sku,
    string ItemName,
    TrackingType TrackingType,
    decimal Ordered,
    decimal Reserved,
    decimal Outstanding,
    decimal AvailableStock,
    IReadOnlyList<ReservationDto> Reservations);

public sealed record ReservationDto(
    long Id,
    decimal RemainingQuantity,
    decimal UnitCostSnapshot,
    DateTimeOffset CreatedAt,
    string CreatedByUserId);

public sealed record ReservationResult(
    long ReservationId,
    long PurchaseOrderLineId,
    decimal QuantityChanged,
    decimal RemainingToReserve,
    decimal ReservationRemaining,
    decimal AvailableStock,
    decimal CostSnapshot);

public sealed record WarehouseCommittedValueDto(
    long WarehouseId,
    string WarehouseName,
    decimal CommittedValue);

public sealed record UserContext(string UserId, string UserName) : ICurrentUser;
