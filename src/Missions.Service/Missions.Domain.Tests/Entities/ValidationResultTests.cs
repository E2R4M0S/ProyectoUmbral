using FluentAssertions;
using Missions.Domain.Entities;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class ValidationResultTests
{
    [Fact]
    public void Success_ReturnsValidResult()
    {
        var result = ValidationResult.Success();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ReturnsInvalidResultWithError()
    {
        var result = ValidationResult.Failure("Content is required");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Be("Content is required");
    }

    [Fact]
    public void Combine_ValidPlusValid_ReturnsValid()
    {
        var valid1 = ValidationResult.Success();
        var valid2 = ValidationResult.Success();

        var combined = ValidationResult.Combine(valid1, valid2);

        combined.IsValid.Should().BeTrue();
        combined.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Combine_ValidPlusInvalid_ReturnsInvalid()
    {
        var valid = ValidationResult.Success();
        var invalid = ValidationResult.Failure("Stage requires at least one clue");

        var combined = ValidationResult.Combine(valid, invalid);

        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().HaveCount(1);
        combined.Errors[0].Should().Be("Stage requires at least one clue");
    }

    [Fact]
    public void Combine_InvalidPlusInvalid_MergesErrors()
    {
        var invalid1 = ValidationResult.Failure("Error 1");
        var invalid2 = ValidationResult.Failure("Error 2");

        var combined = ValidationResult.Combine(invalid1, invalid2);

        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().HaveCount(2);
        combined.Errors.Should().Contain("Error 1");
        combined.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void Combine_EmptyArray_ReturnsValid()
    {
        var combined = ValidationResult.Combine();

        combined.IsValid.Should().BeTrue();
        combined.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Combine_MultipleResults_AggregatesAllErrors()
    {
        var valid = ValidationResult.Success();
        var invalid1 = ValidationResult.Failure("Error A");
        var invalid2 = ValidationResult.Failure("Error B");
        var invalid3 = ValidationResult.Failure("Error C");

        var combined = ValidationResult.Combine(valid, invalid1, invalid2, invalid3);

        combined.IsValid.Should().BeFalse();
        combined.Errors.Should().HaveCount(3);
    }
}