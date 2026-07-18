using FluentAssertions;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Exceptions;
using Missions.Application.Common.Interfaces;
using Missions.Application.Participants.Profile;
using Missions.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Participants.Profile;

public class UpdateProfileCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly ILogger<UpdateProfileCommandHandler> _logger =
        Substitute.For<ILogger<UpdateProfileCommandHandler>>();
    private readonly UpdateProfileCommandHandler _sut;

    public UpdateProfileCommandHandlerTests()
    {
        _sut = new UpdateProfileCommandHandler(_keycloakAdminService, _participantRepository, _logger);
    }

    [Fact]
    public async Task Handle_WhenParticipantNotFound_ShouldThrow()
    {
        var command = new UpdateProfileCommand("Juan", "Perez", "jugador1", "kc-1");
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns((Participant?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_WhenAliasNotUnique_ShouldThrowRegistrationExceptionWithoutCallingKeycloak()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-1");
        var command = new UpdateProfileCommand("Juan", "Perez", "jugador2", "kc-1");
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(participant);
        _participantRepository.IsAliasUniqueAsync("jugador2", "kc-1", Arg.Any<CancellationToken>()).Returns(false);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<RegistrationException>();
        ex.Which.Field.Should().Be("Alias");
        await _keycloakAdminService.DidNotReceiveWithAnyArgs().UpdateUserAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateKeycloakThenParticipant()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-1");
        var command = new UpdateProfileCommand("Juan Carlos", "Perez Gomez", "jugador2", "kc-1");
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(participant);
        _participantRepository.IsAliasUniqueAsync("jugador2", "kc-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FirstName.Should().Be("Juan Carlos");
        result.LastName.Should().Be("Perez Gomez");
        result.Alias.Should().Be("jugador2");
        await _keycloakAdminService.Received(1).UpdateUserAsync("kc-1", "Juan Carlos", "Perez Gomez", "jugador2", Arg.Any<CancellationToken>());
        await _participantRepository.Received(1).UpdateAsync(participant, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDbUpdateFailsAfterKeycloakSucceeded_ShouldRethrow()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-1");
        var command = new UpdateProfileCommand("Juan", "Perez", "jugador2", "kc-1");
        _participantRepository.GetByKeycloakUserIdAsync("kc-1", Arg.Any<CancellationToken>()).Returns(participant);
        _participantRepository.IsAliasUniqueAsync("jugador2", "kc-1", Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.UpdateAsync(Arg.Any<Participant>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB down")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB down");
        await _keycloakAdminService.Received(1).UpdateUserAsync("kc-1", "Juan", "Perez", "jugador2", Arg.Any<CancellationToken>());
    }
}
