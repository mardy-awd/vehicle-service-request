namespace VehicleService.Api.Caching;

public interface IDashboardCacheInvalidator
{
    Task InvalidateAsync(
        string ownerId,
        string? providerId,
        CancellationToken cancellationToken = default);
}