using FluentAssertions;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Missions.Catalog;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Catalog;

public class GetMissionByIdQueryHandlerTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();
    private readonly GetMissionByIdQueryHandler _sut;

    public GetMissionByIdQueryHandlerTests()
    {
        _sut = new GetMissionByIdQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenMissionExists_ReturnsMissionDetailDto()
    {
        // Arrange
        var mission = Mission.Create("Find Treasure", "Secret location", Difficulty.Hard, 45, MissionType.Treasure);
        // Use reflection to set status to Active
        typeof(Mission).GetProperty("Status")!.SetValue(mission, MissionStatus.Active);

        _repository.GetByIdAsync(mission.Id, Arg.Any<CancellationToken>())
            .Returns(mission);

        var query = new GetMissionByIdQuery(mission.Id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Treasure");
        result.Description.Should().Be("Secret location");
        result.Difficulty.Should().Be("Hard");
        result.TimeMinutes.Should().Be(45);
        result.Type.Should().Be("Treasure");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        var query = new GetMissionByIdQuery(id);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}