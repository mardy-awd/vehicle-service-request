using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class StartServiceRequestRequest
    {
        [Required]
        [StringLength(2000, MinimumLength = 5)]
        public required string TechnicianNotes { get; init; }

        [Required]
        public DateTime EstimatedCompletionAtUtc { get; init; }
    }
}