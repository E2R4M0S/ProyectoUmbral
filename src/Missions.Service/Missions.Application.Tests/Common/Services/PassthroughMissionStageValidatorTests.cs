using FluentAssertions;
using Missions.Application.Common.Services;
using Xunit;

namespace Missions.Application.Tests.Common.Services;

public class PassthroughMissionStageValidatorTests
{
    [Fact]
    public async Task HasAtLeastOneStageAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var validator = new PassthroughMissionStageValidator();
        var missionId = Guid.NewGuid();

        // Act
        var result = await validator.HasAtLeastOneStageAsync(missionId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAtLeastOneStageAsync_WithDifferentMissionIds_ShouldReturnTrue()
    {
        // Arrange
        var validator = new PassthroughMissionStageValidator();
        var missionId1 = Guid.NewGuid();
        var missionId2 = Guid.NewGuid();

        // Act & Assert
        (await validator.HasAtLeastOneStageAsync(missionId1, CancellationToken.None)).Should().BeTrue();
        (await validator.HasAtLeastOneStageAsync(missionId2, CancellationToken.None)).Should().BeTrue();
    }
}