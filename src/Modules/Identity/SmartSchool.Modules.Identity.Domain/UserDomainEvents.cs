using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed record UserCreatedDomainEvent(
    Guid UserId,
    string Username,
    string Email,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record UserActivatedDomainEvent(Guid UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record UserDeactivatedDomainEvent(Guid UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record RoleAssignedDomainEvent(Guid UserId, Guid RoleId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
