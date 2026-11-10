using VehicleService.Core.Enums;

namespace VehicleService.Core.Entities
{
    public class ServiceRequest
    {
        public Guid Id { get; set; }
        public required string RequestNumber { get; set; }
        public required string OwnerId { get; set; }
        public Guid VehicleId { get; set; }
        public string? AssignedProviderId { get; set; }
        public ServiceType ServiceType { get; set; }
        public required string IssueDescription { get; set; }
        public DateTime PreferredServiceAtUtc { get; set; }
        public ServiceTimeSlot TimeSlot { get; set; }
        public bool PickupRequired { get; set; }
        public string? PickupAddress { get; set; }
        public required string ContactPhoneNumber { get; set; }
        public ServiceRequestStatus Status { get; set; }
        public DateTime? EstimatedCompletionAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public long Version { get; set; }

        public Vehicle Vehicle { get; set; } = null!;

        public ICollection<ServiceRequestStatusHistory> StatusHistories { get; set; } = [];

        public ICollection<ServiceRequestProgress> Progresses { get; set; } = [];

        public ServiceCompletion? Completion { get; set; }
    }
}