using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Exceptions;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Register;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Register;

public class RegisterParticipantCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly IParticipantRepository _participantRepository = Substitute.For<IParticipantRepository>();
    private readonly ILogger<RegisterParticipantCommandHandler> _logger =
        Substitute.For<ILogger<RegisterParticipantCommandHandler>>();
    private readonly RegisterParticipantCommandHandler _sut;

    public RegisterParticipantCommandHandlerTests()
    {
        _sut = new RegisterParticipantCommandHandler(
            _keycloakService,
            _participantRepository,
            _logger);
    }

    [Fact]
    public async Task Handle_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var command = new RegisterParticipantCommand(
            "John", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234");

        _participantRepository
            .IsAliasUniqueAsync("johnny", Arg.Any<CancellationToken>())
            .Returns(true);

        _participantRepository
            .IsEmailUniqueAsync("john@test.com", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .CreateUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns("keycloak-user-id");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        await _keycloakService.Received(1).CreateUserAsync(
            command.Username,
            command.Email,
            command.Password,
            command.FirstName,
            command.LastName,
            command.Alias,
            Arg.Any<CancellationToken>());

        await _participantRepository.Received(1).AddAsync(
            Arg.Is<Participant>(p =>
                p.FirstName == command.FirstName &&
                p.LastName == command.LastName &&
                p.Alias == command.Alias &&
                p.Email == command.Email &&
                p.KeycloakUserId == "keycloak-user-id"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowWhenAliasNotUnique()
    {
        // Arrange
        var command = new RegisterParticipantCommand(
            "John", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234");

        _participantRepository
            .IsAliasUniqueAsync("johnny", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RegistrationException>()
            .Where(e => e.Field == "Alias");
    }

    [Fact]
    public async Task Handle_ShouldThrowWhenEmailNotUnique()
    {
        // Arrange
        var command = new RegisterParticipantCommand(
            "John", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234");

        _participantRepository
            .IsAliasUniqueAsync("johnny", Arg.Any<CancellationToken>())
            .Returns(true);

        _participantRepository
            .IsEmailUniqueAsync("john@test.com", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RegistrationException>()
            .Where(e => e.Field == "Email");
    }

    [Fact]
    public async Task Handle_ShouldCompensate_WhenDbSaveFails()
    {
        // Arrange
        var command = new RegisterParticipantCommand(
            "John", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234");

        _participantRepository
            .IsAliasUniqueAsync("johnny", Arg.Any<CancellationToken>())
            .Returns(true);

        _participantRepository
            .IsEmailUniqueAsync("john@test.com", Arg.Any<CancellationToken>())
            .Returns(true);

        _keycloakService
            .CreateUserAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns("keycloak-user-id");

        _participantRepository
            .When(x => x.AddAsync(Arg.Any<Participant>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("DB failure"));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify compensating delete was called with the Keycloak user ID
        await _keycloakService.Received(1).DeleteUserAsync(
            "keycloak-user-id", Arg.Any<CancellationToken>());
    }
}
