using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockReservation.Application;
using StockReservation.Infrastructure.Caching;
using StockReservation.Infrastructure.Options;
using StockReservation.Infrastructure.Persistence;
using StockReservation.Infrastructure.Queries;

namespace StockReservation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<CacheOptions>(configuration.GetSection("Cache"));
        services.AddMemoryCache(options => options.SizeLimit = 1024);

        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IApplicationCache, MemoryApplicationCache>();

        services.AddScoped<PurchaseOrderQueries>();
        services.AddScoped<FinanceQueries>();
        services.AddScoped<IPurchaseOrderQueries, CachedPurchaseOrderQueries>();
        services.AddScoped<IFinanceQueries, CachedFinanceQueries>();

        return services;
    }
}
