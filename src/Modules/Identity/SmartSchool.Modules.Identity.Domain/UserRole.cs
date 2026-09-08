namespace SmartSchool.Modules.Identity.Domain;

public sealed class UserRole
{
    private UserRole(Guid userId, Guid roleId, DateTime assignedAtUtc, Guid? assignedBy)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
        AssignedBy = assignedBy;
    }

    private UserRole() { }

    public Guid UserId { get; private init; }
    public Guid RoleId { get; private init; }
    public DateTime AssignedAtUtc { get; private init; }
    public Guid? AssignedBy { get; private init; }

    internal static UserRole Create(Guid userId, Guid roleId, DateTime assignedAtUtc) =>
        new(userId, roleId, assignedAtUtc, assignedBy: null);
}
