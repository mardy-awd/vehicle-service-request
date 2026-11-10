using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.ServiceRequests
{
    public class CompleteServiceRequestRequest
    {
        [Required]
        [StringLength(2000, MinimumLength = 5)]
        public required string CompletionNotes { get; init; }

        [Range(typeof(decimal), "0", "999999999.99")]
        public decimal? FinalAmount { get; init; }
    }
}