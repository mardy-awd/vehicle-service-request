using Microsoft.AspNetCore.Identity;

namespace VehicleService.Api.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
