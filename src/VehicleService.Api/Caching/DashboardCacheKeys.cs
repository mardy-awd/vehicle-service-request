using VehicleService.Api.Identity;

namespace VehicleService.Api.Caching;

public static class DashboardCacheKeys
{
    public static string For(string userId, string role)
    {
        return role switch
        {
            AppRoles.Admin => "dashboard:admin",
            AppRoles.VehicleOwner =>
                $"dashboard:owner:{userId}",
            AppRoles.ServiceProvider =>
                $"dashboard:provider:{userId}",
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                "Unsupported role.")
        };
    }
}