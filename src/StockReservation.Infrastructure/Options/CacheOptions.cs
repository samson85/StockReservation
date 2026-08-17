namespace StockReservation.Infrastructure.Options;

public sealed class CacheOptions
{
    public int PurchaseOrdersSeconds { get; init; } = 15;
    public int FinanceSeconds { get; init; } = 15;
}
