using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class AssignServiceProviderRequest
    {
        [Required]
        public required string ProviderId { get; init; }

        [MaxLength(500)]
        public string? Notes { get; init; }
    }
}