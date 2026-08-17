# Application and Database Design

## Application layers

```text
React + TypeScript
       |
       | REST / JSON
       v
StockReservation.Api
  - Controllers
  - Request/exception middleware
  - authentication adapter
  - composition root
       |
       v
StockReservation.Application
  - reservation use case
  - query contracts
  - DTOs
  - quantity rules
  - cache abstraction
       |
       v
StockReservation.Infrastructure
  - EF Core DbContext/configurations
  - PostgreSQL repositories
  - read queries
  - memory cache implementation
  - database initializer
       |
       v
PostgreSQL

StockReservation.Domain sits underneath Application and contains no infrastructure dependencies.
```

## Database model

| Table | Purpose | Important constraints |
|---|---|---|
| `warehouses` | Warehouse master | unique identity |
| `categories` | Item categories | required name |
| `inventory_items` | SKU/master item | unique SKU, non-negative standard cost |
| `warehouse_stocks` | On-hand quantity by warehouse/item | unique `(WarehouseId, InventoryItemId)`, non-negative quantity |
| `purchase_orders` | Approved PO header | unique PO number |
| `purchase_order_lines` | Item/quantity requested on a PO | reserved <= ordered |
| `stock_reservations` | Reservation history and active quantity | released <= original, cost snapshot |
| `audit_log_entries` | Immutable reservation/release audit | append-only application behaviour |

### Reservation value

```text
Active quantity = OriginalQuantity - ReleasedQuantity
Committed value = Active quantity * UnitCostSnapshot
```

`UnitCostSnapshot` is written when the reservation is created and is never recalculated from `InventoryItem.StandardCost`.

## Concurrency flow

```text
BEGIN TRANSACTION
    |
    +-- lock warehouse_stock row FOR UPDATE
    |
    +-- lock purchase_order_line row FOR UPDATE
    |
    +-- validate approved PO / quantity / available stock
    |
    +-- create reservation + increment QuantityReserved
    |
    +-- SAVE (gets reservation ID)
    |
    +-- create audit row
    |
    +-- SAVE
    |
COMMIT
    |
    +-- invalidate PO and finance caches
```

For release, the same stock -> PO line ordering is used, followed by a reservation row lock.

## Caching

- `purchase-orders:{warehouseId}`: short-lived read cache.
- `finance:committed-stock-value`: short-lived finance cache.
- Reserve/release invalidates affected keys after commit.

The abstraction is intentionally in Application so the infrastructure implementation can later be replaced by Redis without changing the business logic.

## Observability

Structured `ILogger<T>` logs are emitted for request duration, cache operations, transaction lifecycle, row locks, reservations/releases, and unhandled exceptions. Every HTTP request has an ASP.NET trace ID that is returned in problem responses.
