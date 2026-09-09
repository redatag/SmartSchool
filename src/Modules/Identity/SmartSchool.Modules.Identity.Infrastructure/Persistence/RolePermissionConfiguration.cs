using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "identity");
        builder.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("PK_RolePermissions");
        builder.Property(x => x.RoleId).ValueGeneratedNever();
        builder.Property(x => x.PermissionId).ValueGeneratedNever();
        builder.Property(x => x.AssignedAtUtc).HasColumnType("datetime2").IsRequired();

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RolePermissions_Permissions_PermissionId");
    }
}
