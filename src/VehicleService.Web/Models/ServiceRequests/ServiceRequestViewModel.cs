namespace VehicleService.Web.Models.ServiceRequests;

public sealed class ServiceRequestViewModel
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string? AssignedProviderId { get; set; }

    public string ServiceType { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;

    public DateTime PreferredServiceAtUtc { get; set; }
    public string TimeSlot { get; set; } = string.Empty;

    public bool PickupRequired { get; set; }
    public string? PickupAddress { get; set; }
    public string ContactPhoneNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}