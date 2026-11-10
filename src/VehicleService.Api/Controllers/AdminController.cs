using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleService.Api.Contracts.Admin;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;

namespace VehicleService.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("providers")]
        public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
        {
            var providers = await _userManager.GetUsersInRoleAsync(AppRoles.ServiceProvider);

            var result = providers
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.Email
                })
                .ToList();

            return Ok(result);
        }

        [HttpPost("providers")]
        public async Task<IActionResult> CreateProvider([FromBody] CreateProviderRequest request)
        {
            var email = request.Email.Trim();

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "Email is already registered"
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                return BadRequest(createResult.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.ServiceProvider);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Unable to create provider account" });
            }

            await transaction.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = AppRoles.ServiceProvider,
                message = "Service provider created successfully"
            });
        }
    }
}