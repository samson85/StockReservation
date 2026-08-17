namespace StockReservation.Domain;

public sealed class DomainException(string message) : Exception(message);
public sealed class NotFoundDomainException(string message) : Exception(message);
