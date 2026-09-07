using System.Net.Mail;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class Email : ValueObject
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(IdentityErrors.InvalidEmail);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 320 || !MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
            return Result.Failure<Email>(IdentityErrors.InvalidEmail);

        return Result.Success(new Email(normalized));
    }

    public override string ToString() => Value;
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
}
