using System.ComponentModel.DataAnnotations;

namespace VehicleService.Api.Contracts.Auth
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public required string FullName { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100, MinimumLength = 5)]
        public required string Email { get; set; }
        
        [Required]
        public required string Password { get; set; }
    }
}
