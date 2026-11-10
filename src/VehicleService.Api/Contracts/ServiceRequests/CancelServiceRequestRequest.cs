using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class CancelServiceRequestRequest
    {
        [MaxLength(500)]
        public string? Reason { get; init; }
    }
}