namespace VehicleService.Core.Entities
{
    public class ServiceCompletion
    {
        public Guid Id { get; set; }
        public Guid ServiceRequestId { get; set; }
        public required string CompletedByUserId { get; set; }
        public required string CompletionNotes { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public decimal? FinalAmount { get; set; }

        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}