using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Application;

public static class IdentityApplicationErrors
{
    public static readonly Error EmailAlreadyExists = new("identity.email_exists", "A user with this email already exists.");
    public static readonly Error WeakPassword = new("identity.weak_password", "The password must contain at least 8 characters, including upper-case, lower-case and a number.");
    public static readonly Error InvalidCredentials = new("identity.invalid_credentials", "The email or password is invalid.");
    public static readonly Error UserNotFound = new("identity.user_not_found", "The user was not found.");
    public static readonly Error RoleNotFound = new("identity.role_not_found", "The role was not found.");
    public static readonly Error RoleAlreadyExists = new("identity.role_exists", "A role with this name already exists.");
}
