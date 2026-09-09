using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSchool.Modules.Identity.Application;

namespace SmartSchool.Modules.Identity.Presentation;

[ApiController]
[Route("api/identity/roles")]
public sealed class RolesController(CreateRoleCommandHandler handler) : ControllerBase
{
    [HttpPost]
    [RequirePermission("roles.manage")]
    [ProducesResponseType<CreateRoleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateRoleCommand(request.SchoolId, request.Name, request.Description),
            cancellationToken);

        if (result.IsSuccess)
        {
            var response = new CreateRoleResponse(result.RoleId!.Value);
            return Created($"/api/identity/roles/{response.RoleId}", response);
        }

        if (result.ValidationErrors.Count > 0)
        {
            var errors = result.ValidationErrors
                .GroupBy(x => x.Field)
                .ToDictionary(group => group.Key, group => group.Select(x => x.Message).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed."
            });
        }

        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: result.ErrorCode,
            detail: "A role with this name already exists in the school.");
    }
}

public sealed record CreateRoleRequest(Guid SchoolId, string Name, string? Description);
