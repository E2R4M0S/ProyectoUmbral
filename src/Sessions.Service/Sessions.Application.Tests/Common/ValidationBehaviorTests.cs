using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Sessions.Application.Common.Behaviors;
using Xunit;

namespace Sessions.Application.Tests.Common;

// Minimal request/response for testing
public record TestRequest(string Value) : IRequest<string>;

public class ValidationBehaviorTests
{
    private readonly RequestHandlerDelegate<string> _next;

    public ValidationBehaviorTests()
    {
        _next = Substitute.For<RequestHandlerDelegate<string>>();
        _next.Invoke().Returns("ok");
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNextAndReturnsResult()
    {
        var sut = new ValidationBehavior<TestRequest, string>([]);

        var result = await sut.Handle(new TestRequest("x"), _next, CancellationToken.None);

        result.Should().Be("ok");
        await _next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WithPassingValidator_CallsNextAndReturnsResult()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult());

        var sut = new ValidationBehavior<TestRequest, string>([validator]);

        var result = await sut.Handle(new TestRequest("valid"), _next, CancellationToken.None);

        result.Should().Be("ok");
        await _next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ThrowsValidationException()
    {
        var failure = new ValidationFailure("Value", "Value is required");
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult([failure]));

        var sut = new ValidationBehavior<TestRequest, string>([validator]);

        var act = async () => await sut.Handle(new TestRequest(""), _next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_AggregatesAllErrors()
    {
        var f1 = new ValidationFailure("Value", "Error 1");
        var f2 = new ValidationFailure("Value", "Error 2");

        var v1 = Substitute.For<IValidator<TestRequest>>();
        v1.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult([f1]));

        var v2 = Substitute.For<IValidator<TestRequest>>();
        v2.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult([f2]));

        var sut = new ValidationBehavior<TestRequest, string>([v1, v2]);

        var act = async () => await sut.Handle(new TestRequest(""), _next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithMixedValidators_OneFailsOnePass_ThrowsException()
    {
        var passingValidator = Substitute.For<IValidator<TestRequest>>();
        passingValidator.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult());

        var failingValidator = Substitute.For<IValidator<TestRequest>>();
        failingValidator.Validate(Arg.Any<ValidationContext<TestRequest>>())
            .Returns(new ValidationResult([new ValidationFailure("Value", "Bad")]));

        var sut = new ValidationBehavior<TestRequest, string>([passingValidator, failingValidator]);

        var act = async () => await sut.Handle(new TestRequest("x"), _next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _next.DidNotReceive().Invoke();
    }
}
