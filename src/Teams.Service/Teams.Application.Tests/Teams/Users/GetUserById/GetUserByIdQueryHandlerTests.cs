using System.Collections.Immutable;
using FluentAssertions;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Users.GetUsers;
using Teams.Application.Teams.Users.GetUserById;
using Xunit;

namespace Teams.Application.Tests.Teams.Users.GetUserById;

public class GetUserByIdQueryHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly GetUserByIdQueryHandler _sut;

    public GetUserByIdQueryHandlerTests()
    {
        _sut = new GetUserByIdQueryHandler(_keycloakService);
    }

    private static long Ts(DateTime dt) => new DateTimeOffset(dt).ToUnixTimeMilliseconds();

    private static UserRepresentation User(
        string id, string firstName, string email, bool enabled,
        long createdTimestamp, bool emailVerified = false,
        params string[] roles)
    {
        return new UserRepresentation
        {
            Id = id,
            FirstName = firstName,
            Email = email,
            Enabled = enabled,
            CreatedTimestamp = createdTimestamp,
            EmailVerified = emailVerified,
            Attributes = ImmutableDictionary.Create<string, string[]>()
        };
    }

    [Fact]
    public async Task Handle_UserExists_ReturnsUserDetailWithRoles()
    {
        // Arrange
        var query = new GetUserByIdQuery("user-123");
        var user = User("user-123", "Juan", "juan@test.com", enabled: true,
            Ts(DateTime.Parse("2024-01-01")), emailVerified: true);
        var roles = new[] { "admin", "operator" };

        _keycloakService.GetUserByIdAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);
        _keycloakService.GetUserRealmRolesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(roles);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.Name.Should().Be("Juan");
        result.Email.Should().Be("juan@test.com");
        result.Enabled.Should().BeTrue();
        result.EmailVerified.Should().BeTrue();
        result.Roles.Should().Contain("admin");
        result.Roles.Should().Contain("operator");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNull()
    {
        // Arrange
        var query = new GetUserByIdQuery("nonexistent");

        _keycloakService.GetUserByIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((UserRepresentation?)null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}