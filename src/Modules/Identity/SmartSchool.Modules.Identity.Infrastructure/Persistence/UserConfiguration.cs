using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");
        builder.HasKey(x => x.UserId).HasName("PK_Users");
        builder.Property(x => x.UserId).HasColumnName("UserId").ValueGeneratedNever();
        builder.Property(x => x.SchoolId).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedUsername).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnType("datetime2");
        builder.Property<bool>("EmailConfirmed").HasDefaultValue(false).IsRequired();
        builder.Property<bool>("PhoneConfirmed").HasDefaultValue(false).IsRequired();
        builder.Property<DateTime?>("ModifiedAtUtc").HasColumnType("datetime2");
        builder.Property<byte[]>("Version").IsRowVersion().IsRequired();

        builder.HasIndex(x => new { x.SchoolId, x.NormalizedUsername })
            .IsUnique().HasDatabaseName("UX_Users_School_NormalizedUsername");
        builder.HasIndex(x => new { x.SchoolId, x.NormalizedEmail })
            .IsUnique().HasDatabaseName("UX_Users_School_NormalizedEmail");
        builder.HasIndex(x => new { x.SchoolId, x.Status })
            .HasDatabaseName("IX_Users_SchoolId_Status");
        builder.HasMany(x => x.UserRoles)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.UserRoles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(x => x.DomainEvents);
    }
}
