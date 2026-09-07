using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class IdentityModelTests
{
    [Theory]
    [InlineData(typeof(User), "Users")]
    [InlineData(typeof(Role), "Roles")]
    [InlineData(typeof(PermissionRecord), "Permissions")]
    [InlineData(typeof(UserRole), "UserRoles")]
    [InlineData(typeof(RolePermission), "RolePermissions")]
    [InlineData(typeof(RefreshToken), "RefreshTokens")]
    public void Entity_UsesExpectedIdentityTable(Type clrType, string tableName)
    {
        using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(clrType);

        Assert.NotNull(entityType);
        Assert.Equal(tableName, entityType.GetTableName());
        Assert.Equal("identity", entityType.GetSchema());
    }

    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Role))]
    [InlineData(typeof(RefreshToken))]
    public void MutableAggregateData_UsesRowVersionConcurrency(Type clrType)
    {
        using var dbContext = CreateDbContext();

        var rowVersion = dbContext.Model.FindEntityType(clrType)!.FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void NaturalAndJoinKeys_AreUnique()
    {
        using var dbContext = CreateDbContext();

        AssertUniqueIndex(dbContext, typeof(User), nameof(User.Username));
        AssertUniqueIndex(dbContext, typeof(Email), nameof(Email.Value));
        AssertUniqueIndex(dbContext, typeof(Role), nameof(Role.Name));
        AssertUniqueIndex(dbContext, typeof(PermissionRecord), nameof(PermissionRecord.Code));
        AssertUniqueIndex(dbContext, typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId));
        AssertUniqueIndex(dbContext, typeof(RolePermission), nameof(RolePermission.RoleId), nameof(RolePermission.Code));
        AssertUniqueIndex(dbContext, typeof(RefreshToken), nameof(RefreshToken.TokenHash));
    }

    [Fact]
    public void ForeignKeys_StayInsideIdentityModule()
    {
        using var dbContext = CreateDbContext();

        var foreignKeys = dbContext.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()).ToArray();

        Assert.NotEmpty(foreignKeys);
        Assert.All(foreignKeys, foreignKey =>
            Assert.StartsWith("SmartSchool.Modules.Identity.", foreignKey.PrincipalEntityType.ClrType.Namespace ?? string.Empty));
    }

    private static void AssertUniqueIndex(IdentityDbContext dbContext, Type clrType, params string[] propertyNames)
    {
        var entityType = dbContext.Model.FindEntityType(clrType)!;
        var index = entityType.GetIndexes().SingleOrDefault(candidate =>
            candidate.Properties.Select(x => x.Name).SequenceEqual(propertyNames));
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    private static IdentityDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SmartSchoolModelTests;Trusted_Connection=True")
            .Options);
}
