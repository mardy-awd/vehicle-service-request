using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class RejectServiceRequestRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 5)]
        public required string Reason { get; init; }
    }
}
