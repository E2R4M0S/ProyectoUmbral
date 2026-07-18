using FluentAssertions;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Operators.Enable;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Operators.Enable;

public class EnableOperatorCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<EnableOperatorCommandHandler> _logger =
        Substitute.For<ILogger<EnableOperatorCommandHandler>>();
    private readonly EnableOperatorCommandHandler _sut;

    public EnableOperatorCommandHandlerTests()
    {
        _sut = new EnableOperatorCommandHandler(_keycloakAdminService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnKeycloakResult()
    {
        var command = new EnableOperatorCommand("carlos@umbral.local");
        var expected = new EnableOperatorResponse("Operador activado correctamente.", false);
        _keycloakAdminService.EnableOperatorAsync("carlos@umbral.local", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WhenAlreadyEnabled_ShouldReturnMessageIndicatingSo()
    {
        var command = new EnableOperatorCommand("carlos@umbral.local");
        var expected = new EnableOperatorResponse("El operador ya estaba activo.", true);
        _keycloakAdminService.EnableOperatorAsync("carlos@umbral.local", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.WasAlreadyEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenKeycloakThrows_ShouldRethrow()
    {
        var command = new EnableOperatorCommand("noexiste@umbral.local");
        _keycloakAdminService.EnableOperatorAsync("noexiste@umbral.local", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EnableOperatorResponse>(new InvalidOperationException("No user found")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
