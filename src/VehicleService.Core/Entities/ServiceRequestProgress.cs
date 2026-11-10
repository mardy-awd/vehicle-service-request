namespace VehicleService.Core.Entities
{
    public class ServiceRequestProgress
    {
        public Guid Id { get; set; }
        public Guid ServiceRequestId { get; set; }
        public required string ProviderId { get; set; }
        public required string TechnicianNotes { get; set; }
        public DateTime? EstimatedCompletionAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}