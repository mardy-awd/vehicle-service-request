using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleService.Api.Authentication;
using VehicleService.Api.Contracts.Auth;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _context;

        public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService, AppDbContext context)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var email = request.Email.Trim();

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return Conflict(new { message = "Email is already registered" });
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.VehicleOwner);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(roleResult.Errors);
            }

            await transaction.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                userId = user.Id,
                email = user.Email,
                role = AppRoles.VehicleOwner,
                message = "User registered successfully"
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (!user.IsActive || await _userManager.IsLockedOutAsync(user))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                var result = await _userManager.AccessFailedAsync(user);

                if (!result.Succeeded)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { message = "Unable to process login request." });
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }

            var resetResult = await _userManager.ResetAccessFailedCountAsync(user);

            if (!resetResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Unable to process login request." });
            }

            var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user);

            return Ok(tokenResponse);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "User not found" });
            }
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Count != 1)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = "Invalid user role configuration" });
            }

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = roles[0],
                isActive = user.IsActive
            });
        }
    }
}
