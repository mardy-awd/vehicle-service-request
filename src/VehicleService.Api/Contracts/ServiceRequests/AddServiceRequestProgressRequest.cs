using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class AddServiceRequestProgressRequest
    {
        [Required]
        [StringLength(2000, MinimumLength = 5)]
        public required string TechnicianNotes { get; init; }

        public DateTime? EstimatedCompletionAtUtc { get; init; }
    }
}