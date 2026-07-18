using System.Collections.Immutable;
using FluentAssertions;
using Missions.Application.Common.Interfaces;
using Missions.Application.Participants.Profile;
using Missions.Application.Users.GetUsers;
using Missions.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Participants.Profile;

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
    public async Task Handle_WhenParticipantExists_ShouldReturnResponseWithoutCallingKeycloak()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-1");
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(participant);

        var result = await _sut.Handle(new GetProfileQuery("kc-1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Alias.Should().Be("jugador1");
        await _keycloakAdmin.DidNotReceiveWithAnyArgs().GetUserByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenParticipantMissingAndKeycloakUserMissing_ShouldReturnNull()
    {
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((Participant?)null);
        _keycloakAdmin.GetUserByIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((UserRepresentation?)null);

        var result = await _sut.Handle(new GetProfileQuery("kc-1"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenParticipantMissingButKeycloakUserHasAliasAttribute_ShouldRecreateParticipantWithThatAlias()
    {
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((Participant?)null);
        var keycloakUser = new UserRepresentation
        {
            Id = "kc-1",
            FirstName = "Juan",
            Email = "juan@example.com",
            Attributes = ImmutableDictionary<string, string[]>.Empty.Add("alias", new[] { "jugador1" })
        };
        _keycloakAdmin.GetUserByIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(keycloakUser);

        var result = await _sut.Handle(new GetProfileQuery("kc-1"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Alias.Should().Be("jugador1");
        await _participantRepository.Received(1).AddAsync(
            Arg.Is<Participant>(p => p.Alias == "jugador1" && p.KeycloakUserId == "kc-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenKeycloakUserHasNoAliasAttribute_ShouldDeriveAliasFromEmail()
    {
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((Participant?)null);
        var keycloakUser = new UserRepresentation
        {
            Id = "kc-1",
            FirstName = "Juan",
            Email = "juanperez@example.com",
            Attributes = ImmutableDictionary<string, string[]>.Empty
        };
        _keycloakAdmin.GetUserByIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(keycloakUser);

        var result = await _sut.Handle(new GetProfileQuery("kc-1"), CancellationToken.None);

        result!.Alias.Should().Be("juanperez");
    }

    [Fact]
    public async Task Handle_WhenKeycloakUserHasNoAliasAndNoEmail_ShouldDefaultAliasToParticipante()
    {
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((Participant?)null);
        var keycloakUser = new UserRepresentation
        {
            Id = "kc-1",
            FirstName = "Juan",
            Email = "",
            Attributes = ImmutableDictionary<string, string[]>.Empty
        };
        _keycloakAdmin.GetUserByIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(keycloakUser);

        var result = await _sut.Handle(new GetProfileQuery("kc-1"), CancellationToken.None);

        result!.Alias.Should().Be("participante");
        result.Email.Should().Be("participante@umbral.local");
    }
}
