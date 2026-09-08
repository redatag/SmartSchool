using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSchool.Modules.Identity.Application;

namespace SmartSchool.Modules.Identity.Presentation;

[ApiController]
[Route("api/identity/auth")]
public sealed class AuthenticationController(LoginCommandHandler handler) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginCommand(request.SchoolId, request.UsernameOrEmail, request.Password),
            cancellationToken);

        if (result.Status == LoginStatus.Success)
        {
            return Ok(new LoginResponse(
                result.AccessToken!,
                result.ExpiresAtUtc!.Value,
                result.UserId!.Value,
                result.Username!,
                result.Roles,
                result.Permissions));
        }

        if (result.Status == LoginStatus.Invalid)
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

        if (result.Status == LoginStatus.Forbidden)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: result.ErrorCode,
                detail: "The account is not active.");
        }

        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: result.ErrorCode,
            detail: "The supplied credentials are invalid.");
    }
}

public sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Username,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
