namespace VehicleService.Api.Contracts.ServiceRequests
{
    public sealed class DashboardSummaryResponse
    {
        public int TotalRequests { get; init; }
        public int PendingRequests { get; init; }
        public int AssignedRequests { get; init; }
        public int AcceptedRequests { get; init; }
        public int InProgressRequests { get; init; }
        public int CompletedRequests { get; init; }
        public int ClosedRequests { get; init; }
        public int CancelledRequests { get; init; }

        public int ActiveRequests { get; init; }

        public int? TotalVehicles { get; init; }
    }
}