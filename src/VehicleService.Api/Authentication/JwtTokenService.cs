using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VehicleService.Api.Contracts.Auth;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Authentication
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly UserManager<ApplicationUser> _userManager;

        public JwtTokenService(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager)
        {
            _jwtOptions = options.Value;
            _userManager = userManager;
        }

        public async Task<AuthResponse> GenerateTokenAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Count != 1)
            {
                throw new InvalidOperationException("User must have one role assigned");
            }

            var role = userRoles[0];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email?? throw new InvalidOperationException("User email is missing")),
                new Claim(ClaimTypes.Role, role)
            };

            var keyBytes = Convert.FromBase64String(_jwtOptions.Key);

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);

            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

            var jwtToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials
            );

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            var accessToken = tokenHandler.WriteToken(jwtToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresAtUtc = expiresAtUtc,
                UserId = user.Id,
                Email = user.Email!,
                Role = role
            };
        }
    }
}
