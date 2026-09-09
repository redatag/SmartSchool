namespace SmartSchool.Modules.Identity.Domain;

public sealed class RolePermission
{
    private RolePermission(Guid roleId, Guid permissionId, DateTime assignedAtUtc)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        AssignedAtUtc = assignedAtUtc;
    }

    private RolePermission() { }

    public Guid RoleId { get; private init; }
    public Guid PermissionId { get; private init; }
    public DateTime AssignedAtUtc { get; private init; }

    internal static RolePermission Create(Guid roleId, Guid permissionId, DateTime assignedAtUtc) =>
        new(roleId, permissionId, assignedAtUtc);
}
