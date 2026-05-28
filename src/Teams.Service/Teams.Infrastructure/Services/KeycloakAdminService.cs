using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teams.Application.Common.Interfaces;

namespace Teams.Infrastructure.Services;

public class KeycloakAdminService : IKeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakAdminOptions _options;
    private readonly ILogger<KeycloakAdminService> _logger;

    public KeycloakAdminService(
        HttpClient httpClient,
        IOptions<KeycloakAdminOptions> options,
        ILogger<KeycloakAdminService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreateUserAsync(
        string username, string email, string password, string? alias, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var userId = await CreateKeycloakUserAsync(token, username, email, password, alias, ct);

        await AssignParticipantRoleAsync(token, userId, ct);

        return userId;
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", "admin-cli"),
            new KeyValuePair<string, string>("username", _options.Username),
            new KeyValuePair<string, string>("password", _options.Password),
            new KeyValuePair<string, string>("grant_type", "password")
        });

        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/realms/master/protocol/openid-connect/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak token acquisition failed: {StatusCode} {Error}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("access_token").GetString()!;
    }

    private async Task<string> CreateKeycloakUserAsync(
        string token, string username, string email, string password, string? alias, CancellationToken ct)
    {
        var payload = BuildUserPayload(username, email, password, alias);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak create user failed: {StatusCode} {Error}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        // Extract user ID from Location header: /admin/realms/{realm}/users/{uuid}
        var location = response.Headers.Location?.ToString();
        var userId = location?.Split('/').LastOrDefault();

        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Failed to extract Keycloak user ID from Location header.");

        return userId;
    }

    private async Task AssignParticipantRoleAsync(string token, string userId, CancellationToken ct)
    {
        // Get available realm roles
        var rolesRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/roles");
        rolesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rolesResponse = await _httpClient.SendAsync(rolesRequest, ct);
        rolesResponse.EnsureSuccessStatusCode();

        var roles = await rolesResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var participantRole = roles.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("name").GetString() == "participant");

        if (participantRole.ValueKind == JsonValueKind.Undefined)
        {
            _logger.LogWarning("Participant role not found in realm '{Realm}'", _options.Realm);
            return;
        }

        // Assign the role
        var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { participantRole })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var assignResponse = await _httpClient.SendAsync(assignRequest, ct);

        if (!assignResponse.IsSuccessStatusCode)
        {
            var errorBody = await assignResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to assign participant role to user {UserId}: {StatusCode} {Error}",
                userId, assignResponse.StatusCode, errorBody);
            throw new InvalidOperationException(
                $"Failed to assign participant role to Keycloak user '{userId}': {assignResponse.StatusCode}");
        }
    }

    public async Task DeleteUserAsync(string userId, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to clean up Keycloak user {UserId}: {StatusCode}",
                userId, response.StatusCode);
        }
    }

    public async Task UpdateUserAsync(string userId, string name, string alias, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var payload = new
        {
            firstName = name,
            attributes = new Dictionary<string, string[]>
            {
                ["alias"] = new[] { alias }
            }
        };

        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak update user failed: {StatusCode} {Error}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }
    }

    private static object BuildUserPayload(string username, string email, string password, string? alias)
    {
        var credentials = new[]
        {
            new
            {
                type = "password",
                value = password,
                temporary = false
            }
        };

        if (alias is not null)
        {
            return new
            {
                username,
                email,
                emailVerified = true,
                enabled = true,
                credentials,
                attributes = new Dictionary<string, string[]>
                {
                    ["alias"] = new[] { alias }
                }
            };
        }

        return new
        {
            username,
            email,
            emailVerified = true,
            enabled = true,
            credentials
        };
    }
}
