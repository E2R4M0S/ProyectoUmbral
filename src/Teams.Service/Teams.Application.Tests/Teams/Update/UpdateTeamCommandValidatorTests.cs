using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Update;
using Xunit;

namespace Teams.Application.Tests.Teams.Update;

public class UpdateTeamCommandValidatorTests
{
    private readonly UpdateTeamCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new UpdateTeamCommand(
            Id: Guid.NewGuid(),
            Name: "Los Leones",
            Description: "Equipo de desarrollo",
            AddMemberIds: new List<string> { "user-1" },
            RemoveMemberIds: null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NameRequired_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTeamCommand(
            Id: Guid.NewGuid(),
            Name: "",
            Description: "Description",
            AddMemberIds: null,
            RemoveMemberIds: null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_NameExceeds200Chars_ShouldHaveError()
    {
        // Arrange
        var longName = new string('A', 201);
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            longName,
            "Description",
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_NameAtMaxLength_ShouldBeValid()
    {
        // Arrange
        var maxName = new string('A', 200);
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            maxName,
            "Description",
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DescriptionRequired_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            "Name",
            "",
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required");
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_ShouldBeValid()
    {
        // Arrange
        var maxDescription = new string('D', 2000);
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            "Name",
            maxDescription,
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DescriptionExceeds2000Chars_ShouldHaveError()
    {
        // Arrange
        var longDescription = new string('D', 2001);
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            "Name",
            longDescription,
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters");
    }

    [Fact]
    public void Validate_IdEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTeamCommand(
            Guid.Empty,
            "Name",
            "Description",
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Id is required");
    }

    [Fact]
    public void Validate_NullLists_ShouldBeValid()
    {
        // Arrange
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            "Name",
            "Description",
            null,
            null);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
