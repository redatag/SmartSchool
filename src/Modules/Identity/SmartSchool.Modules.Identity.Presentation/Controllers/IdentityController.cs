using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Presentation.Authorization;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Presentation.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, [FromServices] CreateUserHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(request.Email, request.DisplayName, request.Password), cancellationToken);
        return result.IsSuccess ? Created($"api/identity/users/{result.Value.Id}", result.Value) : FromError(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, [FromServices] LoginHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(request.Email, request.Password), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error, StatusCodes.Status401Unauthorized);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, [FromServices] RefreshTokenHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(request.RefreshToken), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error, StatusCodes.Status401Unauthorized);
    }

    [HasPermission(Permissions.IdentityRolesManage)]
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request, [FromServices] CreateRoleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(request.Name), cancellationToken);
        return result.IsSuccess ? Created($"api/identity/roles/{result.Value.Id}", result.Value) : FromError(result.Error);
    }

    [HasPermission(Permissions.IdentityRolesManage)]
    [HttpPut("roles/{roleId:guid}/permissions/{permissionCode}")]
    public async Task<IActionResult> GrantPermission(Guid roleId, string permissionCode, [FromServices] GrantPermissionHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(roleId, permissionCode), cancellationToken);
        return result.IsSuccess ? NoContent() : FromError(result.Error);
    }

    [HasPermission(Permissions.IdentityRolesManage)]
    [HttpPut("users/{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, [FromServices] AssignRoleHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(userId, roleId), cancellationToken);
        return result.IsSuccess ? NoContent() : FromError(result.Error);
    }

    private ObjectResult FromError(Error error, int statusCode = StatusCodes.Status400BadRequest) =>
        Problem(statusCode: statusCode, title: error.Code, detail: error.Description);
}

public sealed record CreateUserRequest(string Email, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record CreateRoleRequest(string Name);
