using StockReservation.Domain;

namespace StockReservation.Application;

public static class QuantityRules
{
    public static void Validate(InventoryItem item, decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (item.TrackingType == TrackingType.Unit && decimal.Truncate(quantity) != quantity)
            throw new DomainException("Unit-tracked items must use whole-number quantities.");

        if (item.TrackingType == TrackingType.Weight && decimal.Round(quantity, 3) != quantity)
            throw new DomainException("Weight-tracked quantities support at most 3 decimal places.");
    }
}
