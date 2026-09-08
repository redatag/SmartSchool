using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "identity");
        builder.HasKey(x => new { x.UserId, x.RoleId }).HasName("PK_UserRoles");
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.Property(x => x.RoleId).ValueGeneratedNever();
        builder.Property(x => x.AssignedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.AssignedBy);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_UserRoles_Roles_RoleId");
    }
}
