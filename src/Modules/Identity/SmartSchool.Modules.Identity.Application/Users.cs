using SmartSchool.Modules.Identity.Domain;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Application;

public sealed record CreateUserCommand(string Email, string DisplayName, string Password);
public sealed record UserResponse(Guid Id, string Email, string DisplayName);

public sealed class CreateUserHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure) return Result.Failure<UserResponse>(email.Error);
        if (!IsStrongPassword(command.Password)) return Result.Failure<UserResponse>(IdentityApplicationErrors.WeakPassword);
        if (await users.EmailExistsAsync(email.Value.Value, cancellationToken))
            return Result.Failure<UserResponse>(IdentityApplicationErrors.EmailAlreadyExists);

        var userResult = User.Create(email.Value.Value, command.DisplayName, passwordHasher.Hash(command.Password));
        if (userResult.IsFailure) return Result.Failure<UserResponse>(userResult.Error);

        users.Add(userResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new UserResponse(userResult.Value.Id, userResult.Value.Email.Value, userResult.Value.DisplayName));
    }

    private static bool IsStrongPassword(string password) =>
        password is { Length: >= 8 } && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
}
