using FluentAssertions;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Catalog;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Catalog;

public class GetMissionsQueryHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly GetMissionsQueryHandler _sut;

    public GetMissionsQueryHandlerTests()
    {
        _sut = new GetMissionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsItemsAndTotalCount()
    {
        // Arrange
        var missions = new List<Mission>
        {
            Mission.Create("Mission A", "Desc A", Difficulty.Easy, 10, MissionType.Treasure),
            Mission.Create("Mission B", "Desc B", Difficulty.Medium, 20, MissionType.Trivia),
        };

        _repository.GetMissionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 2));

        var query = new GetMissionsQuery(null, null, null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_AppliesSearchFilter_CaseInsensitive()
    {
        // Arrange
        var missions = new List<Mission>
        {
            Mission.Create("Find Treasure", "Desc", Difficulty.Easy, 10, MissionType.Treasure),
        };

        _repository.GetMissionsAsync("treasure", null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 1));

        var query = new GetMissionsQuery("treasure", null, null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Find Treasure");

        await _repository.Received(1).GetMissionsAsync(
            "treasure", null, null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppliesDifficultyFilter()
    {
        // Arrange
        var missions = new List<Mission>
        {
            Mission.Create("Hard Mission", "Desc", Difficulty.Hard, 45, MissionType.Treasure),
        };

        _repository.GetMissionsAsync(null, "Hard", null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 1));

        var query = new GetMissionsQuery(null, "Hard", null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Difficulty.Should().Be("Hard");

        await _repository.Received(1).GetMissionsAsync(
            null, "Hard", null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppliesStatusFilter()
    {
        // Arrange
        var mission = Mission.Create("Active Mission", "Desc", Difficulty.Medium, 20, MissionType.Trivia);
        // Set status to Active via reflection since Mission.Create defaults to Draft
        typeof(Mission).GetProperty("Status")!.SetValue(mission, MissionStatus.Active);

        var missions = new List<Mission> { mission };

        _repository.GetMissionsAsync(null, null, "Active", 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 1));

        var query = new GetMissionsQuery(null, null, "Active", 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Active");

        await _repository.Received(1).GetMissionsAsync(
            null, null, "Active", 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var missions = new List<Mission>();
        for (int i = 0; i < 5; i++)
        {
            missions.Add(Mission.Create($"Mission {i}", $"Desc {i}", Difficulty.Easy, 10, MissionType.Treasure));
        }

        _repository.GetMissionsAsync(null, null, null, 2, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 15));

        var query = new GetMissionsQuery(null, null, null, 2, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ReturnsEmptyListAndZeroTotalCount()
    {
        // Arrange
        _repository.GetMissionsAsync("nonexistent", null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Mission>().AsReadOnly(), 0));

        var query = new GetMissionsQuery("nonexistent", null, null, 1, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithInvalidPage_DefaultsToPage1()
    {
        // Arrange
        var missions = new List<Mission>
        {
            Mission.Create("Mission A", "Desc A", Difficulty.Easy, 10, MissionType.Treasure),
        };

        _repository.GetMissionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 1));

        var query = new GetMissionsQuery(null, null, null, -5, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(1);

        await _repository.Received(1).GetMissionsAsync(
            null, null, null, 1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_DefaultsTo10()
    {
        // Arrange
        var missions = new List<Mission>
        {
            Mission.Create("Mission A", "Desc A", Difficulty.Easy, 10, MissionType.Treasure),
        };

        _repository.GetMissionsAsync(null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((missions.AsReadOnly(), 1));

        var query = new GetMissionsQuery(null, null, null, 1, 0);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.PageSize.Should().Be(10);

        await _repository.Received(1).GetMissionsAsync(
            null, null, null, 1, 10, Arg.Any<CancellationToken>());
    }
}