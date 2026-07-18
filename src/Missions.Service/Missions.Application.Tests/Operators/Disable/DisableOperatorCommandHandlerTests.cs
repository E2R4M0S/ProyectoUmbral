using FluentAssertions;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Operators.Disable;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Operators.Disable;

public class DisableOperatorCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<DisableOperatorCommandHandler> _logger =
        Substitute.For<ILogger<DisableOperatorCommandHandler>>();
    private readonly DisableOperatorCommandHandler _sut;

    public DisableOperatorCommandHandlerTests()
    {
        _sut = new DisableOperatorCommandHandler(_keycloakAdminService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnKeycloakResult()
    {
        var command = new DisableOperatorCommand("carlos@umbral.local");
        var expected = new DisableOperatorResponse("Operador desactivado correctamente.", false);
        _keycloakAdminService.DisableOperatorAsync("carlos@umbral.local", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WhenAlreadyDisabled_ShouldReturnMessageIndicatingSo()
    {
        var command = new DisableOperatorCommand("carlos@umbral.local");
        var expected = new DisableOperatorResponse("El operador ya estaba desactivado.", true);
        _keycloakAdminService.DisableOperatorAsync("carlos@umbral.local", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.WasAlreadyDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenKeycloakThrows_ShouldRethrow()
    {
        var command = new DisableOperatorCommand("noexiste@umbral.local");
        _keycloakAdminService.DisableOperatorAsync("noexiste@umbral.local", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DisableOperatorResponse>(new InvalidOperationException("No user found")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
