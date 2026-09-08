using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidUsernameAndPassword_Succeeds()
    {
        var fixture = new LoginFixture();

        var result = await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal(fixture.User.UserId, result.UserId);
        Assert.Equal("admin", result.Username);
        Assert.Equal("access-token", result.AccessToken);
    }

    [Fact]
    public async Task Handle_ValidEmailAndPassword_Succeeds()
    {
        var fixture = new LoginFixture();

        var result = await fixture.HandleAsync("admin@smartschool.com", "StrongPassword123!");

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("ADMIN@SMARTSCHOOL.COM", fixture.Repository.LastNormalizedCredential);
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsInvalidCredentials()
    {
        var fixture = new LoginFixture(userExists: false);

        var result = await fixture.HandleAsync("unknown", "StrongPassword123!");

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal("identity.invalid_credentials", result.ErrorCode);
        Assert.False(fixture.PasswordHasher.VerifyCalled);
    }

    [Fact]
    public async Task Handle_IncorrectPassword_ReturnsInvalidCredentials()
    {
        var fixture = new LoginFixture();

        var result = await fixture.HandleAsync("admin", "incorrect");

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Equal("identity.invalid_credentials", result.ErrorCode);
        Assert.False(fixture.TokenProvider.WasCalled);
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsForbidden()
    {
        var fixture = new LoginFixture();
        fixture.User.Deactivate();

        var result = await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.Equal(LoginStatus.Forbidden, result.Status);
        Assert.False(fixture.PasswordHasher.VerifyCalled);
    }

    [Fact]
    public async Task Handle_SuspendedUser_ReturnsForbidden()
    {
        var fixture = new LoginFixture();
        fixture.User.Suspend();

        var result = await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.Equal(LoginStatus.Forbidden, result.Status);
        Assert.False(fixture.PasswordHasher.VerifyCalled);
    }

    [Fact]
    public async Task Handle_CallsTokenProviderAfterSuccessfulVerification()
    {
        var fixture = new LoginFixture();

        await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.Equal(["verify", "token"], fixture.Calls);
    }

    [Fact]
    public async Task Handle_UpdatesLastLoginAtUtc()
    {
        var fixture = new LoginFixture();
        var beforeLoginUtc = DateTime.UtcNow;

        await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.NotNull(fixture.User.LastLoginAtUtc);
        Assert.InRange(fixture.User.LastLoginAtUtc.Value, beforeLoginUtc, DateTime.UtcNow);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_IncludesRoles()
    {
        var fixture = new LoginFixture();

        var result = await fixture.HandleAsync("admin", "StrongPassword123!");

        Assert.Equal(["SchoolAdmin"], result.Roles);
        Assert.Empty(result.Permissions);
    }

    private sealed class LoginFixture
    {
        public LoginFixture(bool userExists = true)
        {
            User = CreateUser();
            Repository = new FakeUserRepository(userExists ? User : null);
            PasswordHasher = new FakePasswordHasher(Calls);
            TokenProvider = new FakeTokenProvider(Calls);
            UnitOfWork = new FakeUnitOfWork();
            Handler = new LoginCommandHandler(
                new LoginCommandValidator(),
                Repository,
                PasswordHasher,
                new FakeUserAccessReader(),
                TokenProvider,
                UnitOfWork);
        }

        public List<string> Calls { get; } = [];
        public User User { get; }
        public FakeUserRepository Repository { get; }
        public FakePasswordHasher PasswordHasher { get; }
        public FakeTokenProvider TokenProvider { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        private LoginCommandHandler Handler { get; }

        public Task<LoginResult> HandleAsync(string usernameOrEmail, string password) =>
            Handler.HandleAsync(
                new LoginCommand(User.SchoolId, usernameOrEmail, password),
                CancellationToken.None);

        private static User CreateUser() => User.Create(
            Guid.NewGuid(),
            "admin",
            "ADMIN",
            "admin@smartschool.com",
            "ADMIN@SMARTSCHOOL.COM",
            "hashed-password",
            "Ahmed",
            "Mohamed");
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public string? LastNormalizedCredential { get; private set; }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(user?.UserId == userId ? user : null);

        public Task<User?> GetByUsernameOrEmailAsync(
            Guid schoolId,
            string normalizedUsernameOrEmail,
            CancellationToken cancellationToken)
        {
            LastNormalizedCredential = normalizedUsernameOrEmail;
            var matches = user is not null &&
                          user.SchoolId == schoolId &&
                          (user.NormalizedUsername == normalizedUsernameOrEmail ||
                           user.NormalizedEmail == normalizedUsernameOrEmail);
            return Task.FromResult(matches ? user : null);
        }

        public Task<bool> UsernameExistsAsync(
            Guid schoolId,
            string normalizedUsername,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> EmailExistsAsync(
            Guid schoolId,
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(User addedUser) => throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher(List<string> calls) : IPasswordHasher
    {
        public bool VerifyCalled { get; private set; }

        public string Hash(string password) => throw new NotSupportedException();

        public bool Verify(string hashedPassword, string providedPassword)
        {
            VerifyCalled = true;
            calls.Add("verify");
            return hashedPassword == "hashed-password" && providedPassword == "StrongPassword123!";
        }
    }

    private sealed class FakeUserAccessReader : IUserAccessReader
    {
        public Task<UserAccess> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(new UserAccess(["SchoolAdmin"], []));
    }

    private sealed class FakeTokenProvider(List<string> calls) : ITokenProvider
    {
        public bool WasCalled { get; private set; }

        public AccessTokenResult GenerateAccessToken(
            User user,
            IReadOnlyCollection<string> roles,
            IReadOnlyCollection<string> permissions)
        {
            WasCalled = true;
            calls.Add("token");
            return new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(30));
        }
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
