using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Teams.Infrastructure;
using Teams.Infrastructure.Services;
using Xunit;

namespace Teams.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for KeycloakAdminService using a queued MockHttpMessageHandler.
/// Each test enqueues responses in the order the service will consume them:
/// first the token endpoint, then the actual Keycloak Admin API call(s).
/// </summary>
public class KeycloakAdminServiceTests
{
    private static readonly KeycloakAdminOptions Options = new()
    {
        BaseUrl = "http://keycloak:8080",
        Username = "admin",
        Password = "admin123",
        Realm = "umbral"
    };

    private static KeycloakAdminService Build(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(Options), NullLogger<KeycloakAdminService>.Instance);

    private static HttpResponseMessage TokenResponse() =>
        Json("""{"access_token":"test-token","expires_in":300}""");

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var msg = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return msg;
    }

    private static HttpResponseMessage EmptyOk() => new(HttpStatusCode.OK);

    // ── CreateUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_Success_ReturnsUserId()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse()); // token
        var createResp = new HttpResponseMessage(HttpStatusCode.Created);
        createResp.Headers.Location = new Uri("http://keycloak:8080/admin/realms/umbral/users/user-abc");
        handler.Enqueue(createResp); // create user
        handler.Enqueue(Json("""[{"id":"r1","name":"participant"}]""")); // get roles
        handler.Enqueue(EmptyOk()); // assign role

        var svc = Build(handler);
        var result = await svc.CreateUserAsync("john", "john@test.com", "pass", "John", "Doe", "jdoe", CancellationToken.None);

        result.Should().Be("user-abc");
    }

    [Fact]
    public async Task CreateUserAsync_TokenFails_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var svc = Build(handler);
        await svc.Invoking(s => s.CreateUserAsync("u", "e@e.com", "p", "U", "U", null, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreateUserAsync_CreateFails_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", Encoding.UTF8, "text/plain")
        });

        var svc = Build(handler);
        await svc.Invoking(s => s.CreateUserAsync("u", "e@e.com", "p", "U", "U", null, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreateUserAsync_NoLocationHeader_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Created)); // no Location
        handler.Enqueue(Json("""[{"id":"r1","name":"participant"}]"""));
        handler.Enqueue(EmptyOk());

        var svc = Build(handler);
        await svc.Invoking(s => s.CreateUserAsync("u", "e@e.com", "p", "U", "U", null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Keycloak user ID*");
    }

    // ── UpdateUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_Success_Completes()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(EmptyOk()); // PUT user

        var svc = Build(handler);
        await svc.Invoking(s => s.UpdateUserAsync("u1", "John", "Doe", "jdoe", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateUserAsync_Fails_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("forbidden", Encoding.UTF8, "text/plain")
        });

        var svc = Build(handler);
        await svc.Invoking(s => s.UpdateUserAsync("u1", "J", "Doe", "j", CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>();
    }

    // ── DeleteUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_Success_Completes()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NoContent));

        var svc = Build(handler);
        await svc.Invoking(s => s.DeleteUserAsync("u1", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteUserAsync_DeleteFails_DoesNotThrow()
    {
        // Non-2xx delete is logged as warning but not thrown
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        var svc = Build(handler);
        await svc.Invoking(s => s.DeleteUserAsync("u1", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    // ── CreateOperatorAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOperatorAsync_Success_ReturnsResult()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        var createResp = new HttpResponseMessage(HttpStatusCode.Created);
        createResp.Headers.Location = new Uri("http://keycloak:8080/admin/realms/umbral/users/op-123");
        handler.Enqueue(createResp);
        handler.Enqueue(Json("""[{"id":"r2","name":"operator"}]"""));
        handler.Enqueue(EmptyOk());
        handler.Enqueue(TokenResponse()); // second token for execute-actions-email
        handler.Enqueue(EmptyOk());        // execute-actions-email

        var svc = Build(handler);
        var result = await svc.CreateOperatorAsync("Alice", "alice@test.com", CancellationToken.None);

        result.Email.Should().Be("alice@test.com");
        result.KeycloakUserId.Should().Be("op-123");
    }

    [Fact]
    public async Task CreateOperatorAsync_EmailConflict_ThrowsWithMessage()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("conflict", Encoding.UTF8, "text/plain")
        });

        var svc = Build(handler);
        await svc.Invoking(s => s.CreateOperatorAsync("A", "dup@test.com", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*dup@test.com*");
    }

    // ── DisableOperatorAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DisableOperatorAsync_ActiveUser_ReturnsDisabledMessage()
    {
        var userJson = """[{"id":"u1","firstName":"Bob","email":"bob@test.com","enabled":true,"createdTimestamp":0,"emailVerified":true}]""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(userJson));      // search
        handler.Enqueue(EmptyOk());            // disable
        handler.Enqueue(EmptyOk());            // logout

        var svc = Build(handler);
        var result = await svc.DisableOperatorAsync("bob@test.com", CancellationToken.None);

        result.WasAlreadyDisabled.Should().BeFalse();
        result.Message.Should().Contain("desactivado correctamente");
    }

    [Fact]
    public async Task DisableOperatorAsync_AlreadyDisabledUser_ReturnsAlreadyDisabledMessage()
    {
        var userJson = """[{"id":"u1","firstName":"Bob","email":"bob@test.com","enabled":false,"createdTimestamp":0,"emailVerified":true}]""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(userJson));
        handler.Enqueue(EmptyOk());
        handler.Enqueue(EmptyOk());

        var svc = Build(handler);
        var result = await svc.DisableOperatorAsync("bob@test.com", CancellationToken.None);

        result.WasAlreadyDisabled.Should().BeTrue();
        result.Message.Should().Contain("ya estaba desactivado");
    }

    [Fact]
    public async Task DisableOperatorAsync_UserNotFound_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json("[]")); // empty array — no user found

        var svc = Build(handler);
        await svc.Invoking(s => s.DisableOperatorAsync("ghost@test.com", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ghost@test.com*");
    }

    // ── GetUsersAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        var usersJson = """[{"id":"u1","firstName":"Alice","email":"alice@test.com","enabled":true,"createdTimestamp":1000,"emailVerified":true},{"id":"u2","firstName":"Bob","email":"bob@test.com","enabled":false,"createdTimestamp":2000,"emailVerified":false}]""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(usersJson));

        var svc = Build(handler);
        var users = await svc.GetUsersAsync(0, 10, null, null, CancellationToken.None);

        users.Should().HaveCount(2);
        users[0].Email.Should().Be("alice@test.com");
        users[1].Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetUsersAsync_EmptyResult_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json("[]"));

        var svc = Build(handler);
        var users = await svc.GetUsersAsync(0, 10, null, null, CancellationToken.None);

        users.Should().BeEmpty();
    }

    // ── GetUserByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_Existing_ReturnsUser()
    {
        var json = """{"id":"u1","firstName":"Carol","email":"carol@test.com","enabled":true,"createdTimestamp":0,"emailVerified":true}""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(json));

        var svc = Build(handler);
        var user = await svc.GetUserByIdAsync("u1", CancellationToken.None);

        user.Should().NotBeNull();
        user!.Email.Should().Be("carol@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_NotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        var svc = Build(handler);
        var user = await svc.GetUserByIdAsync("missing", CancellationToken.None);

        user.Should().BeNull();
    }

    // ── GetUsersByRoleAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersByRoleAsync_ReturnsUsers()
    {
        var json = """[{"id":"u1","firstName":"Dave","email":"dave@test.com","enabled":true,"createdTimestamp":0,"emailVerified":true}]""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(json));

        var svc = Build(handler);
        var users = await svc.GetUsersByRoleAsync("operator", 0, 10, CancellationToken.None);

        users.Should().HaveCount(1);
        users[0].Email.Should().Be("dave@test.com");
    }

    // ── GetUserRealmRolesAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserRealmRolesAsync_ReturnsRoleNames()
    {
        var json = """[{"id":"r1","name":"admin"},{"id":"r2","name":"operator"}]""";

        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json(json));

        var svc = Build(handler);
        var roles = await svc.GetUserRealmRolesAsync("u1", CancellationToken.None);

        roles.Should().Contain("admin");
        roles.Should().Contain("operator");
        roles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserRealmRolesAsync_EmptyArray_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(TokenResponse());
        handler.Enqueue(Json("[]"));

        var svc = Build(handler);
        var roles = await svc.GetUserRealmRolesAsync("u1", CancellationToken.None);

        roles.Should().BeEmpty();
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_responses.Count == 0)
            throw new InvalidOperationException($"No more HTTP responses queued for {request.RequestUri}");
        return Task.FromResult(_responses.Dequeue());
    }
}
