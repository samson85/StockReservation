using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockReservation.Application;
using StockReservation.Domain;
using StockReservation.Infrastructure.Persistence;

namespace StockReservation.Infrastructure.Queries;

public class FinanceQueries( AppDbContext db, ILogger<FinanceQueries> logger) : IFinanceQueries
{
    public async Task<IReadOnlyList<WarehouseCommittedValueDto>> GetCommittedValuesAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying committed stock value by warehouse.");

        var commitedValueResults = await db.Warehouses
        .AsNoTracking()
        .Select(w => new
        {
            WarehouseId = w.Id,
            WarehouseName = w.Name,
            CommittedValue = db.StockReservations
                .Where(r => r.WarehouseStock.WarehouseId == w.Id)
                .Sum(r =>
                    (decimal?)(
                        (r.OriginalQuantity - r.ReleasedQuantity)
                        * r.UnitCostSnapshot
                    )) ?? 0m
        })
        .OrderBy(x => x.WarehouseName)
        .ToListAsync(cancellationToken);

            var result = commitedValueResults
                .Select(x => new WarehouseCommittedValueDto(
                    x.WarehouseId,
                    x.WarehouseName,
                    x.CommittedValue))
                .ToList();

        logger.LogDebug("Committed stock value query returned {WarehouseCount} warehouses.", result.Count);

        return result;
    }

    private static bool IsActiveReservation( StockReservationEntity reservation)
    {
        return reservation.OriginalQuantity > reservation.ReleasedQuantity;
    }
}
