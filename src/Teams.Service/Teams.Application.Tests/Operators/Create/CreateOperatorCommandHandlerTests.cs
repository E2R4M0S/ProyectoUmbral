using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Operators.Create;
using Xunit;

namespace Teams.Application.Tests.Operators.Create;

public class CreateOperatorCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<CreateOperatorCommandHandler> _logger =
        Substitute.For<ILogger<CreateOperatorCommandHandler>>();
    private readonly CreateOperatorCommandHandler _sut;

    public CreateOperatorCommandHandlerTests()
    {
        _sut = new CreateOperatorCommandHandler(_keycloakService, _logger);
    }

    [Fact]
    public async Task Handle_ShouldCreateOperatorAndReturnResult()
    {
        // Arrange
        var command = new CreateOperatorCommand("Juan Pérez", "juan@test.com");
        var expectedResult = new CreateOperatorResult("Juan Pérez", "juan@test.com", "kc-user-id");

        _keycloakService
            .CreateOperatorAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Juan Pérez");
        result.Email.Should().Be("juan@test.com");
        result.KeycloakUserId.Should().Be("kc-user-id");

        await _keycloakService.Received(1).CreateOperatorAsync(
            "Juan Pérez",
            "juan@test.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowWhenKeycloakServiceThrows()
    {
        // Arrange
        var command = new CreateOperatorCommand("Juan Pérez", "juan@test.com");

        _keycloakService
            .CreateOperatorAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CreateOperatorResult>(new HttpRequestException("Keycloak unavailable")));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
