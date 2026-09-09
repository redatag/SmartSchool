using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.HasKey(x => x.RoleId).HasName("PK_Roles");
        builder.Property(x => x.RoleId).ValueGeneratedNever();
        builder.Property(x => x.SchoolId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsSystemRole).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property<DateTime?>("ModifiedAtUtc").HasColumnType("datetime2");
        builder.Property<byte[]>("Version").IsRowVersion().IsRequired();

        builder.HasIndex(x => new { x.SchoolId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("UX_Roles_School_NormalizedName");
        builder.HasMany(x => x.RolePermissions)
            .WithOne()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RolePermissions_Roles_RoleId");
        builder.Navigation(x => x.RolePermissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(x => x.DomainEvents);
    }
}
