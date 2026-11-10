using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleService.Api.Contracts.Vehicle;
using VehicleService.Api.Data;
using VehicleService.Api.Identity;
using VehicleService.Core.Entities;

namespace VehicleService.Api.Controllers
{
    [Route("api/vehicles")]
    [ApiController]
    [Authorize(Roles = AppRoles.VehicleOwner)]
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiclesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<VehicleResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return Unauthorized();
            }

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Make = request.Make.Trim(),
                Model = request.Model.Trim(),
                RegistrationNumber = request.RegistrationNumber.Trim(),
                Year = request.Year,
                Mileage = request.Mileage,
                CreatedAtUtc = DateTime.UtcNow
            };
            await _context.Vehicles.AddAsync(vehicle);
            await _context.SaveChangesAsync(cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new VehicleResponse
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                Year = vehicle.Year,
                Mileage = vehicle.Mileage,
                CreatedAtUtc = vehicle.CreatedAtUtc
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<VehicleResponse>>> GetMyVehicles()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(ownerId))
                return Unauthorized();

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.OwnerId == ownerId)
                .OrderByDescending(v => v.CreatedAtUtc)
                .Select(v => new
                {
                    v.Id,
                    v.Make,
                    v.Model,
                    v.RegistrationNumber,
                    v.Year,
                    v.Mileage
                })
                .ToListAsync();

            return Ok(vehicles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleResponse>> GetMyVehicle(Guid id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return Unauthorized();
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.OwnerId == ownerId && v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            var response = new VehicleResponse
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                Year = vehicle.Year,
                Mileage = vehicle.Mileage,
                CreatedAtUtc = vehicle.CreatedAtUtc
            };

            return Ok(response);
        }
    }
}