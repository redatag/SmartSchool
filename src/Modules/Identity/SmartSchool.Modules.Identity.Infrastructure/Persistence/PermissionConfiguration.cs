using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "identity");
        builder.HasKey(x => x.PermissionId).HasName("PK_Permissions");
        builder.Property(x => x.PermissionId).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Permissions_Code");
        builder.HasIndex(x => x.Module).HasDatabaseName("IX_Permissions_Module");

        builder.HasData(
            Seed("20dd65ac-df72-4d75-93e6-357e65c9c101", IdentityPermissionCodes.UsersView, "View users"),
            Seed("20dd65ac-df72-4d75-93e6-357e65c9c102", IdentityPermissionCodes.UsersManage, "Manage users"),
            Seed("20dd65ac-df72-4d75-93e6-357e65c9c103", IdentityPermissionCodes.RolesView, "View roles"),
            Seed("20dd65ac-df72-4d75-93e6-357e65c9c104", IdentityPermissionCodes.RolesManage, "Manage roles"));
    }

    private static object Seed(string id, string code, string name) => new
    {
        PermissionId = Guid.Parse(id),
        Code = code,
        Name = name,
        Module = "Identity",
        Description = (string?)null
    };
}
