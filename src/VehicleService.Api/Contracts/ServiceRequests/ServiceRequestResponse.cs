using VehicleService.Core.Enums;

namespace VehicleService.Api.Contracts.ServiceRequests;

public class ServiceRequestResponse
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string? AssignedProviderId { get; set; }

    public ServiceType ServiceType { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public DateTime PreferredServiceAtUtc { get; set; }
    public ServiceTimeSlot TimeSlot { get; set; }

    public bool PickupRequired { get; set; }
    public string? PickupAddress { get; set; }
    public string ContactPhoneNumber { get; set; } = string.Empty;

    public ServiceRequestStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}