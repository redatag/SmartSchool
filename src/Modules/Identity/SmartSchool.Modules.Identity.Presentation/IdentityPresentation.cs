using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartSchool.Modules.Identity.Application;

namespace SmartSchool.Modules.Identity.Presentation;

public static class IdentityPresentation
{
    public static IMvcBuilder AddIdentityPresentation(this IServiceCollection services) =>
        services.AddControllers().AddApplicationPart(typeof(IdentityPresentation).Assembly);
}

[ApiController]
[Route("api/identity/users")]
public sealed class UsersController(CreateUserCommandHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateUserCommand(
            request.SchoolId,
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber), cancellationToken);

        if (result.IsSuccess)
        {
            var response = new CreateUserResponse(result.UserId!.Value);
            return Created($"/api/identity/users/{response.UserId}", response);
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
            detail: result.ErrorCode == "identity.username_exists"
                ? "A user with this username already exists in the school."
                : "A user with this email already exists in the school.");
    }

    [HttpPost("{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        Guid roleId,
        [FromServices] AssignRoleToUserCommandHandler assignRoleHandler,
        CancellationToken cancellationToken)
    {
        var result = await assignRoleHandler.HandleAsync(
            new AssignRoleToUserCommand(userId, roleId),
            cancellationToken);

        if (result.Status == AssignRoleToUserStatus.Success) return NoContent();

        if (result.Status == AssignRoleToUserStatus.Invalid)
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

        if (result.Status == AssignRoleToUserStatus.NotFound)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.ErrorCode,
                detail: result.ErrorCode == "identity.user_not_found"
                    ? "The user was not found."
                    : "The role was not found.");
        }

        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: result.ErrorCode,
            detail: result.ErrorCode == "identity.role_already_assigned"
                ? "The role is already assigned to the user."
                : "The user and role must belong to the same school.");
    }
}

public sealed record CreateUserRequest(
    Guid SchoolId,
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);
