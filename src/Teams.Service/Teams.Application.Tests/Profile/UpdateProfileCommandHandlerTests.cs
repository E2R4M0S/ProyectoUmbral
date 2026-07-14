using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Exceptions;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Profile;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Profile;

public class UpdateProfileCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly ILogger<UpdateProfileCommandHandler> _logger =
        Substitute.For<ILogger<UpdateProfileCommandHandler>>();
    private readonly UpdateProfileCommandHandler _sut;

    public UpdateProfileCommandHandlerTests()
    {
        _sut = new UpdateProfileCommandHandler(
            _keycloakService,
            _participantRepository,
            _logger);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProfileAndReturnResponse()
    {
        // Arrange
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var command = new UpdateProfileCommand("Jane", "Doe", "jane_alias", "kc-123");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        _participantRepository
            .IsAliasUniqueAsync("jane_alias", "kc-123", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .UpdateUserAsync("kc-123", "Jane", "Doe", "jane_alias", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.Alias.Should().Be("jane_alias");

        await _keycloakService.Received(1).UpdateUserAsync(
            "kc-123", "Jane", "Doe", "jane_alias", Arg.Any<CancellationToken>());

        await _participantRepository.Received(1).UpdateAsync(
            participant, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenParticipantNotFound()
    {
        // Arrange
        var command = new UpdateProfileCommand("Jane", "Doe", "jane_alias", "kc-notfound");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-notfound", Arg.Any<CancellationToken>())
            .Returns((Participant?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Participant not found*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAliasTakenByAnother()
    {
        // Arrange
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var command = new UpdateProfileCommand("Jane", "Doe", "taken_alias", "kc-123");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        _participantRepository
            .IsAliasUniqueAsync("taken_alias", "kc-123", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RegistrationException>()
            .Where(e => e.Field == "Alias");
    }

    [Fact]
    public async Task Handle_ShouldPass_WhenUpdatingOwnAlias()
    {
        // Arrange
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var command = new UpdateProfileCommand("Jane", "Doe", "johnny", "kc-123"); // same alias

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        _participantRepository
            .IsAliasUniqueAsync("johnny", "kc-123", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .UpdateUserAsync("kc-123", "Jane", "Doe", "johnny", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Alias.Should().Be("johnny");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenKeycloakFails()
    {
        // Arrange
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var command = new UpdateProfileCommand("Jane", "Doe", "jane_alias", "kc-123");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        _participantRepository
            .IsAliasUniqueAsync("jane_alias", "kc-123", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .When(x => x.UpdateUserAsync("kc-123", "Jane", "Doe", "jane_alias", Arg.Any<CancellationToken>()))
            .Do(_ => throw new HttpRequestException("Keycloak unavailable"));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowAndLogCritical_WhenDbFailsAfterKeycloakSuccess()
    {
        // Arrange
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var command = new UpdateProfileCommand("Jane", "Doe", "jane_alias", "kc-123");

        _participantRepository
            .GetByKeycloakUserIdAsync("kc-123", Arg.Any<CancellationToken>())
            .Returns(participant);

        _participantRepository
            .IsAliasUniqueAsync("jane_alias", "kc-123", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .UpdateUserAsync("kc-123", "Jane", "Doe", "jane_alias", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _participantRepository
            .When(x => x.UpdateAsync(Arg.Any<Participant>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB failure"));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _logger.Received(1).Log(
            LogLevel.Critical,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
