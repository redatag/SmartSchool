using SmartSchool.SharedKernel;

namespace SmartSchool.SharedKernel.UnitTests;

public sealed class ResultTests
{
    private static readonly Error TestError = new("test.failure", "The operation failed.");

    [Fact]
    public void Error_UsesStructuralEquality()
    {
        Assert.Equal(TestError, new Error("test.failure", "The operation failed."));
        Assert.Equal(string.Empty, Error.None.Code);
    }

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ContainsItsError()
    {
        var result = Result.Failure(TestError);

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesItsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_DoesNotExposeAValue()
    {
        var result = Result.Failure<int>(TestError);

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Theory]
    [InlineData(true, "failure")]
    [InlineData(false, "none")]
    public void Result_RejectsInconsistentState(bool isSuccess, string errorKind)
    {
        var error = errorKind == "none" ? Error.None : TestError;

        Assert.Throws<ArgumentException>(() => new TestResult(isSuccess, error));
    }

    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
