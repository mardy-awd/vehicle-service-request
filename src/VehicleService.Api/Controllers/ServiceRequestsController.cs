using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleService.Api.Contracts.Common;
using VehicleService.Api.Contracts.ServiceRequests;
using VehicleService.Api.Identity;
using VehicleService.Api.Services.Interfaces;
using VehicleService.Api.Services.Results;

namespace VehicleService.Api.Controllers
{
    [Route("api/service-requests")]
    [ApiController]
    [Authorize]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly IServiceRequestService _serviceRequestService;

        public ServiceRequestsController(IServiceRequestService serviceRequestService)
        {
            _serviceRequestService = serviceRequestService;
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.VehicleOwner)]
        public async Task<ActionResult<ServiceRequestResponse>> Create([FromBody] CreateServiceRequestRequest request, CancellationToken cancellationToken)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.Create(ownerId, request, cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.ServiceRequestReaders)]
        public async Task<ActionResult<PagedResult<ServiceRequestResponse>>> GetAll([FromQuery] ServiceRequestQuery query, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var result = await _serviceRequestService.GetAll(userId, role, query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = AppRoles.ServiceRequestReaders)]
        public async Task<ActionResult<ServiceRequestResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var result = await _serviceRequestService.GetById(id, userId, role, cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<ServiceRequestResponse>> AssignProvider(Guid id, [FromBody] AssignServiceProviderRequest request, CancellationToken cancellationToken)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.AssignProvider(
                id,
                adminId,
                request,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/accept")]
        [Authorize(Roles = AppRoles.ServiceProvider)]
        public async Task<ActionResult<ServiceRequestResponse>> Accept(Guid id, CancellationToken cancellationToken)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.Accept(id, providerId, cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = AppRoles.ServiceProvider)]
        public async Task<ActionResult<ServiceRequestResponse>> Reject(Guid id, [FromBody] RejectServiceRequestRequest request, CancellationToken cancellationToken)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(providerId))
                return Unauthorized();

            var result = await _serviceRequestService.Reject(id, providerId, request, cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/start")]
        [Authorize(Roles = AppRoles.ServiceProvider)]
        public async Task<ActionResult<ServiceRequestResponse>> Start(
            Guid id,
            [FromBody] StartServiceRequestRequest request,
            CancellationToken cancellationToken)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.Start(
                id,
                providerId,
                request,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/progress")]
        [Authorize(Roles = AppRoles.ServiceProvider)]
        public async Task<ActionResult<ServiceRequestResponse>> AddProgress(
            Guid id,
            [FromBody] AddServiceRequestProgressRequest request,
            CancellationToken cancellationToken)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.AddProgress(
                id,
                providerId,
                request,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/complete")]
        [Authorize(Roles = AppRoles.ServiceProvider)]
        public async Task<ActionResult<ServiceRequestResponse>> Complete(
            Guid id,
            [FromBody] CompleteServiceRequestRequest request,
            CancellationToken cancellationToken)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(providerId))
            {
                return Unauthorized();
            }

            var result = await _serviceRequestService.Complete(
                id,
                providerId,
                request,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/close")]
        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.VehicleOwner)]
        public async Task<ActionResult<ServiceRequestResponse>> Close(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var result = await _serviceRequestService.Close(
                id,
                userId,
                role,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = AppRoles.Admin + "," + AppRoles.VehicleOwner)]
        public async Task<ActionResult<ServiceRequestResponse>> Cancel(
            Guid id,
            [FromBody] CancelServiceRequestRequest request,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var result = await _serviceRequestService.Cancel(
                id,
                userId,
                role,
                request,
                cancellationToken);

            return ToServiceRequestActionResult(result);
        }

        [HttpGet("{id:guid}/timeline")]
        [Authorize(Roles = AppRoles.ServiceRequestReaders)]
        public async Task<ActionResult<ServiceRequestTimelineResponse>> GetTimeline(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                return Forbid();
            }

            var result = await _serviceRequestService.GetTimeline(
                id,
                userId,
                role,
                cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return result.Error switch
            {
                ServiceError.Validation => BadRequest(new { message = result.ErrorMessage }),

                ServiceError.NotFound => NotFound(new { message = result.ErrorMessage }),

                ServiceError.Forbidden => Forbid(),

                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        private ActionResult<ServiceRequestResponse> ToServiceRequestActionResult(ServiceResult<ServiceRequestResponse> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return result.Error switch
            {
                ServiceError.Validation => BadRequest(new { message = result.ErrorMessage }),

                ServiceError.NotFound => NotFound(new { message = result.ErrorMessage }),

                ServiceError.Conflict => Conflict(new { message = result.ErrorMessage }),

                ServiceError.Forbidden => Forbid(),

                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}