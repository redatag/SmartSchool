namespace SmartSchool.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;
    protected Entity() => Id = default!;

    public TId Id { get; protected init; }
}

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other && GetType() == other.GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, HashCode.Combine);
}

public sealed class DomainException(string message) : Exception(message);
