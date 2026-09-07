using SmartSchool.SharedKernel;

namespace SmartSchool.SharedKernel.UnitTests;

public sealed class DomainPrimitivesTests
{
    [Fact]
    public void Entity_StoresItsIdentifier()
    {
        var id = Guid.NewGuid();

        var entity = new TestEntity(id);

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void AggregateRoot_CollectsAndClearsDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(DateTimeOffset.UtcNow);

        aggregate.Record(domainEvent);

        Assert.Equal([domainEvent], aggregate.DomainEvents);

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void ValueObjects_WithEqualComponentsAreEqual()
    {
        var first = new TestValueObject("value", 7);
        var second = new TestValueObject("value", 7);
        var different = new TestValueObject("other", 7);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void DomainException_PreservesMessage()
    {
        var exception = new DomainException("Business rule failed.");

        Assert.Equal("Business rule failed.", exception.Message);
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Record(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOnUtc) : IDomainEvent;

    private sealed class TestValueObject(string text, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return text;
            yield return number;
        }
    }
}
