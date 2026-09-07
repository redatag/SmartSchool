using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public static class IdentityErrors
{
    public static readonly Error InvalidEmail = new("identity.invalid_email", "A valid email address is required.");
    public static readonly Error InvalidUsername = new("identity.invalid_username", "The username must contain 3 to 50 letters, numbers, dots, underscores or hyphens.");
    public static readonly Error InvalidName = new("identity.invalid_name", "The display name must contain between 2 and 200 characters.");
    public static readonly Error DuplicateRole = new("identity.duplicate_role", "The user already has this role.");
    public static readonly Error RoleNotAssigned = new("identity.role_not_assigned", "The user does not have this role.");
    public static readonly Error UserInactive = new("identity.user_inactive", "A deactivated user cannot be authenticated.");
    public static readonly Error DuplicatePermission = new("identity.duplicate_permission", "The role already has this permission.");
    public static readonly Error InvalidRoleName = new("identity.invalid_role_name", "The role name must contain between 2 and 100 characters.");
    public static readonly Error InvalidPermission = new("identity.invalid_permission", "The permission code is not supported.");
    public static readonly Error InvalidRefreshToken = new("identity.invalid_refresh_token", "The refresh token is invalid or expired.");
}
