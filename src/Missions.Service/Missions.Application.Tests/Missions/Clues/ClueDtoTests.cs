using FluentAssertions;
using Missions.Application.Missions.Clues;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class ClueDtoTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var id = Guid.NewGuid();

        var dto = new ClueDto(id, "Contenido de la pista", 10, "Manual");

        dto.Id.Should().Be(id);
        dto.Content.Should().Be("Contenido de la pista");
        dto.Penalty.Should().Be(10);
        dto.ReleaseType.Should().Be("Manual");
    }

    [Fact]
    public void Constructor_WithNullPenalty_ShouldAllowIt()
    {
        var dto = new ClueDto(Guid.NewGuid(), "Contenido", null, "Auto");

        dto.Penalty.Should().BeNull();
    }
}
