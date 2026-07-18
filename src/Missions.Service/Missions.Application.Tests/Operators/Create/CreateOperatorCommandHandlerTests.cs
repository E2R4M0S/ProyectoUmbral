using FluentAssertions;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;
using Missions.Application.Operators.Create;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Operators.Create;

public class CreateOperatorCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<CreateOperatorCommandHandler> _logger =
        Substitute.For<ILogger<CreateOperatorCommandHandler>>();
    private readonly CreateOperatorCommandHandler _sut;

    public CreateOperatorCommandHandlerTests()
    {
        _sut = new CreateOperatorCommandHandler(_keycloakAdminService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnKeycloakResult()
    {
        var command = new CreateOperatorCommand("Carlos Mendoza", "carlos@umbral.local");
        var expected = new CreateOperatorResult("Carlos Mendoza", "carlos@umbral.local", "kc-op-1");
        _keycloakAdminService.CreateOperatorAsync("Carlos Mendoza", "carlos@umbral.local", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WhenKeycloakThrows_ShouldLogAndRethrow()
    {
        var command = new CreateOperatorCommand("Carlos Mendoza", "carlos@umbral.local");
        _keycloakAdminService.CreateOperatorAsync("Carlos Mendoza", "carlos@umbral.local", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CreateOperatorResult>(new InvalidOperationException("Email already registered")));

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Email already registered");
    }
}
