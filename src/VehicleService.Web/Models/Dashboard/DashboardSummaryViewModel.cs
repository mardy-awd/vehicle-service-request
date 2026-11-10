namespace VehicleService.Web.Models.Dashboard;

public sealed class DashboardSummaryViewModel
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int AssignedRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int ClosedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int ActiveRequests { get; set; }
    public int? TotalVehicles { get; set; }
}