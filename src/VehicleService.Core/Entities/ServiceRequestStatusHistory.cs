using VehicleService.Core.Enums;

namespace VehicleService.Core.Entities
{
    public class ServiceRequestStatusHistory
    {
        public Guid Id { get; set; }
        public Guid ServiceRequestId { get; set; }
        public ServiceRequestStatus? FromStatus { get; set; }
        public ServiceRequestStatus ToStatus { get; set; }
        public required string ChangedByUserId { get; set; }
        public string? RelatedProviderId { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAtUtc { get; set; }

        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}