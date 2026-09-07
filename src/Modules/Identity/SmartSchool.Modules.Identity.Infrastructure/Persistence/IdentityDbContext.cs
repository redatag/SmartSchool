using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IIdentityUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries()
            .Select(x => x.Entity)
            .OfType<AggregateRoot<Guid>>()
            .ToArray();

        var events = aggregates.SelectMany(x => x.DomainEvents).ToArray();
        foreach (var domainEvent in events)
        {
            OutboxMessages.Add(OutboxMessage.From(domainEvent));
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
        return result;
    }
}

public sealed class OutboxMessage
{
    public Guid Id { get; private init; }
    public DateTimeOffset OccurredOnUtc { get; private init; }
    public string Type { get; private init; } = string.Empty;
    public string Content { get; private init; } = string.Empty;
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }

    public static OutboxMessage From(IDomainEvent domainEvent) => new()
    {
        Id = Guid.NewGuid(),
        OccurredOnUtc = domainEvent.OccurredOnUtc,
        Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
        Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
    };
}

public sealed class InboxMessage
{
    public Guid Id { get; private init; }
    public string Consumer { get; private init; } = string.Empty;
    public DateTimeOffset ReceivedOnUtc { get; private init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
