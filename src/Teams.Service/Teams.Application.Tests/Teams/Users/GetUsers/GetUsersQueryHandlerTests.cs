using System.Collections.Immutable;
using FluentAssertions;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Users.GetUsers;
using Xunit;

namespace Teams.Application.Tests.Teams.Users.GetUsers;

public class GetUsersQueryHandlerTests
{
    private readonly IKeycloakAdminService _keycloakService = Substitute.For<IKeycloakAdminService>();
    private readonly GetUsersQueryHandler _sut;

    public GetUsersQueryHandlerTests()
    {
        _sut = new GetUsersQueryHandler(_keycloakService);
    }

    // ─── UserRepresentation helpers ────────────────────────────────────────

    private static long Ts(DateTime dt) => new DateTimeOffset(dt).ToUnixTimeMilliseconds();

    private static UserRepresentation User(
        string id, string firstName, string email, bool enabled, long createdTimestamp,
        string[]? attributes = null, bool emailVerified = false)
    {
        var rep = new UserRepresentation
        {
            Id = id,
            FirstName = firstName,
            Email = email,
            Enabled = enabled,
            CreatedTimestamp = createdTimestamp,
            EmailVerified = emailVerified,
            Attributes = attributes != null
                ? attributes.Select((v, i) => KeyValuePair.Create($"key{i}", new[] { v }))
                    .ToImmutableDictionary()
                : ImmutableDictionary<string, string[]>.Empty
        };
        return rep;
    }

    // ─── No-role path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoRole_ReturnsUsersWithResolvedRoles()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 1, PageSize: 20);
        var users = new List<UserRepresentation>
        {
            User("u1", "Juan", "juan@test.com", true, Ts(DateTime.Parse("2024-01-01"))),
            User("u2", "Maria", "maria@test.com", true, Ts(DateTime.Parse("2024-01-02"))),
        };

        _keycloakService.GetUsersAsync(0, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(users);
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(new[] { "admin" });
        _keycloakService.GetUserRealmRolesAsync("u2", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items[0].Name.Should().Be("Juan");
        result.Items[0].Roles.Should().Contain("admin");
        result.Items[1].Name.Should().Be("Maria");
        result.Items[1].Roles.Should().Contain("operator");
    }

    [Fact]
    public async Task Handle_NoRole_EmptyKeycloakResult_ReturnsEmptyItems()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 1, PageSize: 20);
        _keycloakService.GetUsersAsync(0, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoRole_WithSearch_PassesSearchToKeycloak()
    {
        // Arrange
        var query = new GetUsersQuery(Search: "juan", Role: null, Enabled: null, Page: 1, PageSize: 20);
        _keycloakService.GetUsersAsync(0, 20, "juan", null, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>
            {
User("u1", "Juan", "juan@test.com", true, Ts(DateTime.Parse("2024-01-01")))
            });
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Juan");
    }

    // ─── Role-filter path ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithRole_CallsGetUsersByRole()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: "operator", Enabled: null, Page: 1, PageSize: 20);
        var users = new List<UserRepresentation>
        {
            User("u1", "Carlos", "carlos@test.com", true, Ts(DateTime.Parse("2024-01-01")))
        };

        _keycloakService.GetUsersByRoleAsync("operator", 0, 20, Arg.Any<CancellationToken>())
            .Returns(users);
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _keycloakService.Received(1).GetUsersByRoleAsync("operator", 0, 20, Arg.Any<CancellationToken>());
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Carlos");
    }

    [Fact]
    public async Task Handle_WithRole_AppliesSearchClientSide()
    {
        // Arrange
        var query = new GetUsersQuery(Search: "maria", Role: "operator", Enabled: null, Page: 1, PageSize: 20);
        var users = new List<UserRepresentation>
        {
            User("u1", "Juan", "juan@test.com", true, Ts(DateTime.Parse("2024-01-01"))),
            User("u2", "Maria", "maria@test.com", true, Ts(DateTime.Parse("2024-01-02")))
        };

        _keycloakService.GetUsersByRoleAsync("operator", 0, 20, Arg.Any<CancellationToken>())
            .Returns(users);
        // Both users get roles
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });
        _keycloakService.GetUserRealmRolesAsync("u2", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Maria");
    }

    [Fact]
    public async Task Handle_WithRole_AppliesEnabledFilterClientSide()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: "operator", Enabled: true, Page: 1, PageSize: 20);
        var users = new List<UserRepresentation>
        {
            User("u1", "Active", "active@test.com", enabled: true, Ts(DateTime.Parse("2024-01-01"))),
            User("u2", "Inactive", "inactive@test.com", enabled: false, Ts(DateTime.Parse("2024-01-02")))
        };

        _keycloakService.GetUsersByRoleAsync("operator", 0, 20, Arg.Any<CancellationToken>())
            .Returns(users);
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });
        _keycloakService.GetUserRealmRolesAsync("u2", Arg.Any<CancellationToken>())
            .Returns(new[] { "operator" });

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WithRole_EmptyResult_ReturnsEmptyItems()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: "admin", Enabled: null, Page: 1, PageSize: 20);
        _keycloakService.GetUsersByRoleAsync("admin", 0, 20, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ─── Pagination ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PageSizeClampedTo100()
    {
        // Arrange — pageSize over max should be clamped
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 1, PageSize: 200);
        var users = new List<UserRepresentation>
        {
            User("u1", "Test", "test@test.com", true, Ts(DateTime.Parse("2024-01-01")))
        };

        _keycloakService.GetUsersAsync(0, 100, null, null, Arg.Any<CancellationToken>())
            .Returns(users);
        _keycloakService.GetUserRealmRolesAsync("u1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — verify clamped to 100, not 200
        await _keycloakService.Received(1).GetUsersAsync(0, 100, null, null, Arg.Any<CancellationToken>());
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_PageSizeBelow1_ClampedTo1()
    {
        // Arrange — pageSize < 1 should be clamped to 1
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 1, PageSize: 0);
        _keycloakService.GetUsersAsync(0, 1, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>());
        // Act
        var result = await _sut.Handle(query, CancellationToken.None);
        // Assert
        await _keycloakService.Received(1).GetUsersAsync(0, 1, null, null, Arg.Any<CancellationToken>());
        result.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Page2_ReturnsCorrectFirstOffset()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 2, PageSize: 20);
        _keycloakService.GetUsersAsync(20, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>());
        // Act
        var result = await _sut.Handle(query, CancellationToken.None);
        // Assert — first = (page-1)*pageSize = (2-1)*20 = 20
        await _keycloakService.Received(1).GetUsersAsync(20, 20, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageBeyondTotal_ReturnsEmptyItems()
    {
        // Arrange
        var query = new GetUsersQuery(Search: null, Role: null, Enabled: null, Page: 100, PageSize: 20);
        // Keycloak returns empty (no results beyond actual pages)
        _keycloakService.GetUsersAsync(1980, 20, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<UserRepresentation>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}