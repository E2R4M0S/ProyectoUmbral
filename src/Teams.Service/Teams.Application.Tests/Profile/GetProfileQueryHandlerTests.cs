using FluentAssertions;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Profile;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Profile;

public class GetProfileQueryHandlerTests
{
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly IKeycloakAdminService _keycloakAdmin = Substitute.For<IKeycloakAdminService>();
    private readonly GetProfileQueryHandler _sut;

    public GetProfileQueryHandlerTests()
    {
        _sut = new GetProfileQueryHandler(_participantRepository, _keycloakAdmin);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfile_WhenParticipantExists()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");
        var query = new GetProfileQuery("kc-123");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John Doe");
        result.Alias.Should().Be("johnny");
        result.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenParticipantNotFound()
    {
        // Arrange
        var query = new GetProfileQuery("kc-notfound");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-notfound", Arg.Any<CancellationToken>())
            .Returns((Participant?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}