using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Create;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Create;

public class CreateMissionCommandHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly ILogger<CreateMissionCommandHandler> _logger =
        Substitute.For<ILogger<CreateMissionCommandHandler>>();
    private readonly CreateMissionCommandHandler _sut;

    public CreateMissionCommandHandlerTests()
    {
        _sut = new CreateMissionCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WithUniqueTitle_ShouldCreateMissionAndReturnResult()
    {
        // Arrange
        var command = new CreateMissionCommand(
            "Find the Treasure",
            "Hidden treasure in the forest",
            "Easy",
            30,
            "Treasure");

        _repository.IsTitleUniqueAsync("Find the Treasure", Arg.Any<CancellationToken>())
            .Returns(true);

        _repository.AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Find the Treasure");
        result.Description.Should().Be("Hidden treasure in the forest");
        result.Difficulty.Should().Be("Easy");
        result.TimeMinutes.Should().Be(30);
        result.Type.Should().Be("Treasure");
        result.Status.Should().Be("Draft");

        await _repository.Received(1).IsTitleUniqueAsync("Find the Treasure", Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateTitle_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateMissionCommand(
            "Existing Mission",
            "Description",
            "Medium",
            15,
            "Trivia");

        _repository.IsTitleUniqueAsync("Existing Mission", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        await _repository.Received(1).IsTitleUniqueAsync("Existing Mission", Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithHardDifficulty_ShouldReturnCorrectDifficulty()
    {
        // Arrange
        var command = new CreateMissionCommand(
            "Hard Mission",
            "Very difficult challenge",
            "Hard",
            90,
            "Trivia");

        _repository.IsTitleUniqueAsync("Hard Mission", Arg.Any<CancellationToken>())
            .Returns(true);

        _repository.AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Difficulty.Should().Be("Hard");
    }
}