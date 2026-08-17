using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockReservation.Application;
using StockReservation.Infrastructure.Options;
using StockReservation.Infrastructure.Queries;
using System.Threading;

namespace StockReservation.Infrastructure.Caching;

public sealed class CachedPurchaseOrderQueries( PurchaseOrderQueries inner, IApplicationCache cache, IOptions<CacheOptions> options, ILogger<CachedPurchaseOrderQueries> logger) : IPurchaseOrderQueries
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> GetOutstandingAsync(long warehouseId, CancellationToken cancellationToken)
    {
        var key = CacheKeys.PurchaseOrders(warehouseId);

        if (cache.TryGet<IReadOnlyList<PurchaseOrderDto>>(key, out var cached) && cached is not null)
            return cached;

        logger.LogDebug("Loading outstanding purchase orders from database for warehouse {WarehouseId}", warehouseId);
        var result = await inner.GetOutstandingAsync(warehouseId, cancellationToken);
        cache.Set(key, result, TimeSpan.FromSeconds(Math.Max(1, options.Value.PurchaseOrdersSeconds)));

        return result;
    }
}

public sealed class CachedFinanceQueries( FinanceQueries inner, IApplicationCache cache, IOptions<CacheOptions> options, ILogger<CachedFinanceQueries> logger) : IFinanceQueries
{
    public async Task<IReadOnlyList<WarehouseCommittedValueDto>> GetCommittedValuesAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGet<IReadOnlyList<WarehouseCommittedValueDto>>(CacheKeys.FinanceCommittedStockValue, out var cached) && cached is not null)
            return cached;

        logger.LogDebug("Loading finance committed stock value from database");
        var result = await inner.GetCommittedValuesAsync(cancellationToken);
        cache.Set(CacheKeys.FinanceCommittedStockValue, result, TimeSpan.FromSeconds(Math.Max(1, options.Value.FinanceSeconds)));

        return result;
    }
}
