using System.ComponentModel.DataAnnotations;
using VehicleService.Core.Enums;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class CreateServiceRequestRequest
    {
        [Required]
        public Guid VehicleId { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string IssueDescription { get; set; } = string.Empty;

        [Required]
        public DateTime PreferredServiceAtUtc { get; set; }

        [Required]
        public ServiceTimeSlot TimeSlot { get; set; }

        public bool PickupRequired { get; set; }

        [StringLength(500)]
        public string? PickupAddress { get; set; }

        [Required]
        [Phone]
        public string ContactPhoneNumber { get; set; } = string.Empty;
    }
}
