using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired().IsConcurrencyToken();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(x => x.Value).HasColumnName("Email").HasMaxLength(320).IsRequired();
            email.HasIndex(x => x.Value).IsUnique();
        });
        builder.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RefreshTokens).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired().IsConcurrencyToken();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasMany(x => x.Permissions).WithOne().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("PermissionCode").HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.RoleId, x.Code }).IsUnique();
        builder.HasOne<PermissionRecord>().WithMany().HasForeignKey(x => x.Code)
            .HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<PermissionRecord>
{
    public void Configure(EntityTypeBuilder<PermissionRecord> builder)
    {
        builder.ToTable("Permissions", "identity");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(150).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasData(global::SmartSchool.Modules.Identity.Domain.Permissions.All.OrderBy(x => x, StringComparer.Ordinal)
            .Select(code => new { Code = code, Description = code }));
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired().IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "identity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.HasIndex(x => x.ProcessedOnUtc);
    }
}

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages", "identity");
        builder.HasKey(x => new { x.Id, x.Consumer });
        builder.Property(x => x.Consumer).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);
    }
}
