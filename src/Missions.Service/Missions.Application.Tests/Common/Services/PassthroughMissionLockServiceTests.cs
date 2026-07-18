using FluentAssertions;
using Missions.Application.Common.Services;
using Xunit;

namespace Missions.Application.Tests.Common.Services;

public class PassthroughMissionLockServiceTests
{
    [Fact]
    public async Task IsMissionInUseAsync_ShouldAlwaysReturnFalse()
    {
        var sut = new PassthroughMissionLockService();

        var result = await sut.IsMissionInUseAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }
}
