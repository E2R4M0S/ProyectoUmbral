using FluentAssertions;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class ReleaseTypeEnumTests
{
    [Fact]
    public void ReleaseType_ShouldHaveAutoValue()
    {
        ReleaseType.Auto.Should().BeDefined();
    }

    [Fact]
    public void ReleaseType_ShouldHaveManualValue()
    {
        ReleaseType.Manual.Should().BeDefined();
    }
}
