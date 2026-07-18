using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Trivia.Application.Common.Behaviors;
using Xunit;

namespace Trivia.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record DummyRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<DummyRequest, string>(new List<IValidator<DummyRequest>>());
        var request = new DummyRequest("anything");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be("ok");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPassingValidator_ShouldCallNext()
    {
        var validator = Substitute.For<IValidator<DummyRequest>>();
        validator.Validate(Arg.Any<ValidationContext<DummyRequest>>()).Returns(new ValidationResult());
        var behavior = new ValidationBehavior<DummyRequest, string>(new List<IValidator<DummyRequest>> { validator });
        var request = new DummyRequest("valid");
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ShouldThrowValidationExceptionAndNotCallNext()
    {
        var failures = new List<ValidationFailure> { new("Value", "Value is required") };
        var validator = Substitute.For<IValidator<DummyRequest>>();
        validator.Validate(Arg.Any<ValidationContext<DummyRequest>>()).Returns(new ValidationResult(failures));
        var behavior = new ValidationBehavior<DummyRequest, string>(new List<IValidator<DummyRequest>> { validator });
        var request = new DummyRequest("");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        };

        var act = async () => await behavior.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithMultipleValidatorsAndOneFails_ShouldThrowWithAggregatedFailures()
    {
        var passingValidator = Substitute.For<IValidator<DummyRequest>>();
        passingValidator.Validate(Arg.Any<ValidationContext<DummyRequest>>()).Returns(new ValidationResult());

        var failingValidator = Substitute.For<IValidator<DummyRequest>>();
        failingValidator.Validate(Arg.Any<ValidationContext<DummyRequest>>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Value", "bad value") }));

        var behavior = new ValidationBehavior<DummyRequest, string>(
            new List<IValidator<DummyRequest>> { passingValidator, failingValidator });
        var request = new DummyRequest("x");
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = async () => await behavior.Handle(request, next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "bad value");
    }
}
