using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class RolePermission : Entity<Guid>
{
    internal RolePermission(Guid id, Guid roleId, string code) : base(id)
    {
        RoleId = roleId;
        Code = code;
    }

    private RolePermission() { }

    public Guid RoleId { get; private init; }
    public string Code { get; private init; } = string.Empty;
}
