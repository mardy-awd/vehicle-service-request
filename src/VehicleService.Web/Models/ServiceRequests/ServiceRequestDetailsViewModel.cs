namespace VehicleService.Web.Models.ServiceRequests;

public sealed class ServiceRequestDetailsViewModel
{
    public ServiceRequestViewModel Request { get; set; } = new();

    public ServiceRequestTimelineViewModel Timeline { get; set; } = new();
}