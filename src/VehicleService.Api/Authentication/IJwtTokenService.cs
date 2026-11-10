using VehicleService.Api.Contracts.Auth;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Authentication
{
    public interface IJwtTokenService
    {
        Task<AuthResponse> GenerateTokenAsync(ApplicationUser user);
    }
}
