using Microsoft.EntityFrameworkCore;
using StockReservation.Domain;
using StockReservation.Infrastructure.Persistence.Configurations;

namespace StockReservation.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<StockReservationEntity> StockReservations => Set<StockReservationEntity>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Configure(modelBuilder);
    }

    public override int SaveChanges()
    {
        RejectAuditMutation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectAuditMutation();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void RejectAuditMutation()
    {
        var invalid = ChangeTracker.Entries<AuditLogEntry>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (invalid)
            throw new InvalidOperationException("Audit log entries are immutable.");
    }
}
