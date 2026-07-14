using Nevma.ServiceDefaults.Errors;

namespace Nevma.ServiceDefaults.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_contains_the_expected_error()
    {
        var error = new Error("planning.invitation_not_found", "The invitation was not found.", ErrorType.NotFound);

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_failure_does_not_expose_a_value()
    {
        var result = Result<string>.Failure(new Error("test.failure", "Test failure."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Invalid_result_state_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new TestResult(true, new Error("test.error", "Test error.")));
        Assert.Throws<ArgumentException>(() => new TestResult(false, Error.None));
    }

    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
