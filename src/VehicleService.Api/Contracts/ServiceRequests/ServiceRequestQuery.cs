using System.ComponentModel.DataAnnotations;
using VehicleService.Core.Enums;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class ServiceRequestQuery : IValidatableObject
    {
        public ServiceRequestStatus? Status { get; set; }
        public ServiceType? ServiceType { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDateUtc { get; set; }
        public DateTime? ToDateUtc { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (FromDateUtc.HasValue &&
                ToDateUtc.HasValue &&
                FromDateUtc > ToDateUtc)
            {
                yield return new ValidationResult(
                    "FromDateUtc must not be later than ToDateUtc",
                    [nameof(FromDateUtc), nameof(ToDateUtc)]);
            }
        }
    }
}