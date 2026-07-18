using FluentAssertions;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Exceptions;
using Missions.Application.Common.Interfaces;
using Missions.Application.Participants.Register;
using Missions.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Participants.Register;

public class RegisterParticipantCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly ILogger<RegisterParticipantCommandHandler> _logger =
        Substitute.For<ILogger<RegisterParticipantCommandHandler>>();
    private readonly RegisterParticipantCommandHandler _sut;

    public RegisterParticipantCommandHandlerTests()
    {
        _sut = new RegisterParticipantCommandHandler(_keycloakAdminService, _participantRepository, _logger);
    }

    private static RegisterParticipantCommand ValidCommand() => new(
        "Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "Password123");

    [Fact]
    public async Task Handle_WhenAliasNotUnique_ShouldThrowRegistrationExceptionAndNotCallKeycloak()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(false);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<RegistrationException>();
        ex.Which.Field.Should().Be("Alias");
        await _keycloakAdminService.DidNotReceiveWithAnyArgs().CreateUserAsync(default!, default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenEmailNotUnique_ShouldThrowRegistrationExceptionAndNotCallKeycloak()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<RegistrationException>();
        ex.Which.Field.Should().Be("Email");
        await _keycloakAdminService.DidNotReceiveWithAnyArgs().CreateUserAsync(default!, default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenKeycloakThrowsAlreadyRegistered_ShouldThrowRegistrationExceptionForEmail()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.CreateUserAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName, command.Alias, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("Email already registered in Keycloak.")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<RegistrationException>();
        ex.Which.Field.Should().Be("Email");
    }

    [Fact]
    public async Task Handle_WhenKeycloakThrowsOtherException_ShouldRethrowAsIs()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.CreateUserAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName, command.Alias, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new HttpRequestException("Keycloak unreachable")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndPersistParticipant()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.CreateUserAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName, command.Alias, Arg.Any<CancellationToken>())
            .Returns("kc-user-123");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _participantRepository.Received(1).AddAsync(
            Arg.Is<Participant>(p => p.Alias == "jugador1" && p.KeycloakUserId == "kc-user-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPersistFails_ShouldCleanUpKeycloakUserAndRethrow()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.CreateUserAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName, command.Alias, Arg.Any<CancellationToken>())
            .Returns("kc-user-123");
        _participantRepository.AddAsync(Arg.Any<Participant>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB down")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB down");
        await _keycloakAdminService.Received(1).DeleteUserAsync("kc-user-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPersistFailsAndCleanupAlsoFails_ShouldStillRethrowOriginalException()
    {
        var command = ValidCommand();
        _participantRepository.IsAliasUniqueAsync(command.Alias, Arg.Any<CancellationToken>()).Returns(true);
        _participantRepository.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.CreateUserAsync(command.Username, command.Email, command.Password, command.FirstName, command.LastName, command.Alias, Arg.Any<CancellationToken>())
            .Returns("kc-user-123");
        _participantRepository.AddAsync(Arg.Any<Participant>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB down")));
        _keycloakAdminService.DeleteUserAsync("kc-user-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Cleanup failed too")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB down");
    }
}
