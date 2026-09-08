using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserSuccessfully()
    {
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal(result.UserId, repository.AddedUser.UserId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_RejectsDuplicateUsername()
    {
        var repository = new FakeUserRepository { UsernameExists = true };
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.username_exists", result.ErrorCode);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task Handle_RejectsDuplicateEmail()
    {
        var repository = new FakeUserRepository { EmailExists = true };
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.email_exists", result.ErrorCode);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task Handle_HashesPasswordBeforePersistence()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal("hashed::StrongPassword123!", repository.AddedUser!.PasswordHash);
        Assert.NotEqual("StrongPassword123!", repository.AddedUser.PasswordHash);
    }

    private static CreateUserCommandHandler CreateHandler(FakeUserRepository repository, FakeUnitOfWork unitOfWork) =>
        new(new CreateUserCommandValidator(), repository, new FakePasswordHasher(), unitOfWork);

    private static CreateUserCommand ValidCommand() => new(
        Guid.NewGuid(), "admin", "admin@smartschool.com", "StrongPassword123!", "Ahmed", "Mohamed", "0500000000");

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool UsernameExists { get; init; }
        public bool EmailExists { get; init; }
        public User? AddedUser { get; private set; }
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
        public Task<bool> UsernameExistsAsync(Guid schoolId, string normalizedUsername, CancellationToken cancellationToken) => Task.FromResult(UsernameExists);
        public Task<bool> EmailExistsAsync(Guid schoolId, string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(EmailExists);
        public void Add(User user) => AddedUser = user;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed::{password}";
    }

    private sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
