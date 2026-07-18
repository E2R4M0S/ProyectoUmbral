using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Common.Interfaces;
using Missions.Application.Participants.Password;
using Xunit;

namespace Missions.Application.Tests.Participants.Password;

public class ChangePasswordCommandHandlerTests
{
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<ChangePasswordCommandHandler> _logger = Substitute.For<ILogger<ChangePasswordCommandHandler>>();
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _sut = new ChangePasswordCommandHandler(_keycloakAdminService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldVerifyThenResetPassword()
    {
        var command = new ChangePasswordCommand("OldPass123", "NewPass456", "kc-user-1", "user@test.com");

        await _sut.Handle(command, CancellationToken.None);

        await _keycloakAdminService.Received(1).VerifyPasswordAsync("user@test.com", "OldPass123", Arg.Any<CancellationToken>());
        await _keycloakAdminService.Received(1).ResetPasswordAsync("kc-user-1", "NewPass456", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyNewPassword_ShouldThrowValidationExceptionWithoutCallingKeycloak()
    {
        var command = new ChangePasswordCommand("OldPass123", "   ", "kc-user-1", "user@test.com");

        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _keycloakAdminService.DidNotReceive().VerifyPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakAdminService.DidNotReceive().ResetPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_ShouldPropagateUnauthorizedAndNotReset()
    {
        var command = new ChangePasswordCommand("WrongPass", "NewPass456", "kc-user-1", "user@test.com");
        _keycloakAdminService.VerifyPasswordAsync("user@test.com", "WrongPass", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new UnauthorizedAccessException("La contraseña actual no es correcta."));

        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _keycloakAdminService.DidNotReceive().ResetPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
