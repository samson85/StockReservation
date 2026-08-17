namespace StockReservation.Api.Contracts.Reservations;

public record ReserveRequest(
    long PurchaseOrderLineId,
    decimal Quantity);
