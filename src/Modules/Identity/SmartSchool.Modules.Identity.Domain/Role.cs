using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class Role : AggregateRoot<Guid>
{
    private readonly List<RolePermission> _permissions = [];

    private Role(Guid id, string name) : base(id)
    {
        Name = name;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Role() { }

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Result<Role> Create(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        return normalized.Length is < 2 or > 100
            ? Result.Failure<Role>(IdentityErrors.InvalidRoleName)
            : Result.Success(new Role(Guid.NewGuid(), normalized));
    }

    public Result GrantPermission(string permissionCode)
    {
        if (!global::SmartSchool.Modules.Identity.Domain.Permissions.All.Contains(permissionCode))
            return Result.Failure(IdentityErrors.InvalidPermission);
        if (_permissions.Any(x => x.Code == permissionCode))
            return Result.Failure(IdentityErrors.DuplicatePermission);

        _permissions.Add(new RolePermission(Guid.NewGuid(), Id, permissionCode));
        return Result.Success();
    }
}
