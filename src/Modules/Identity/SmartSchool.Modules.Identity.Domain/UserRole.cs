using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class UserRole : Entity<Guid>
{
    internal UserRole(Guid id, Guid userId, Guid roleId) : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = DateTimeOffset.UtcNow;
    }

    private UserRole() { }

    public Guid UserId { get; private init; }
    public Guid RoleId { get; private init; }
    public DateTimeOffset AssignedAtUtc { get; private init; }
}
