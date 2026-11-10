using VehicleService.Api.Contracts.ServiceRequests;

namespace VehicleService.Api.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetSummary(
            string userId,
            string role,
            CancellationToken cancellationToken = default);
    }
}