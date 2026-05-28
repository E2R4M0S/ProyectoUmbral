using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Operators.Disable;
using Xunit;

namespace Teams.Application.Tests.Teams.Operators.Disable;

public class DisableOperatorCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<DisableOperatorCommandHandler> _logger =
        Substitute.For<ILogger<DisableOperatorCommandHandler>>();
    private readonly DisableOperatorCommandHandler _sut;

    public DisableOperatorCommandHandlerTests()
    {
        _sut = new DisableOperatorCommandHandler(_keycloakService, _logger);
    }

    [Fact]
    public async Task Handle_ShouldDisableOperatorAndReturnResult()
    {
        // Arrange
        var command = new DisableOperatorCommand("juan@test.com");
        var expectedResult = new DisableOperatorResponse("Operador desactivado correctamente.", false);

        _keycloakService
            .DisableOperatorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Operador desactivado correctamente.");
        result.WasAlreadyDisabled.Should().BeFalse();

        await _keycloakService.Received(1).DisableOperatorAsync(
            "juan@test.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnWasAlreadyDisabled_WhenOperatorWasAlreadyDisabled()
    {
        // Arrange
        var command = new DisableOperatorCommand("juan@test.com");
        var expectedResult = new DisableOperatorResponse("El operador ya estaba desactivado.", true);

        _keycloakService
            .DisableOperatorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasAlreadyDisabled.Should().BeTrue();
        result.Message.Should().Be("El operador ya estaba desactivado.");
    }

    [Fact]
    public async Task Handle_ShouldThrowAndLog_WhenKeycloakServiceThrowsNotFound()
    {
        // Arrange
        var command = new DisableOperatorCommand("unknown@test.com");

        _keycloakService
            .DisableOperatorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DisableOperatorResponse>(
                new InvalidOperationException("No user found with email 'unknown@test.com'")));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No user found with email 'unknown@test.com'");
    }

    [Fact]
    public async Task Handle_ShouldRethrow_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var command = new DisableOperatorCommand("juan@test.com");

        _keycloakService
            .DisableOperatorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DisableOperatorResponse>(
                new HttpRequestException("Keycloak unavailable")));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}