namespace SmartSchool.Modules.Identity.Domain;

public sealed class Permission
{
    private Permission(Guid permissionId, string code, string name, string module, string? description)
    {
        PermissionId = permissionId;
        Code = code;
        Name = name;
        Module = module;
        Description = description;
    }

    private Permission() { }

    public Guid PermissionId { get; private init; }
    public string Code { get; private init; } = string.Empty;
    public string Name { get; private init; } = string.Empty;
    public string Module { get; private init; } = string.Empty;
    public string? Description { get; private init; }

    public static Permission Create(string code, string name, string module, string? description = null)
    {
        code = Required(code, nameof(code), 150).ToLowerInvariant();
        name = Required(name, nameof(name), 150);
        module = Required(module, nameof(module), 100);
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > 500)
            throw new IdentityDomainException("Description cannot exceed 500 characters.");
        if (code.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '-')))
            throw new IdentityDomainException("Permission code contains invalid characters.");

        return new Permission(Guid.CreateVersion7(), code, name, module, description);
    }

    private static string Required(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new IdentityDomainException($"{name} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
            throw new IdentityDomainException($"{name} cannot exceed {maximumLength} characters.");
        return trimmed;
    }
}

public static class IdentityPermissionCodes
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
}
