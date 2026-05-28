using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Operators.Create;
using Teams.Application.Teams.Operators.Disable;

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

        var location = response.Headers.Location?.ToString();
        var userId = location?.Split('/').LastOrDefault();

        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Failed to extract Keycloak user ID from Location header.");

        return userId;
    }

    private async Task AssignParticipantRoleAsync(string token, string userId, CancellationToken ct)
    {
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

    // ─── Operator-specific methods ────────────────────────────────────────

    private async Task<string> CreateKeycloakOperatorUserAsync(
        string token, string name, string email, string password, CancellationToken ct)
    {
        var payload = BuildOperatorPayload(name, email, password);

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
                "Keycloak create operator failed: {StatusCode} {Error}",
                response.StatusCode, errorBody);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"Email '{email}' is already registered in Keycloak.");
            }

            response.EnsureSuccessStatusCode();
        }

        var location = response.Headers.Location?.ToString();
        var userId = location?.Split('/').LastOrDefault();

        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Failed to extract Keycloak user ID from Location header.");

        return userId;
    }

    private static object BuildOperatorPayload(string name, string email, string password)
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

        return new
        {
            username = email,
            email,
            emailVerified = true,
            enabled = true,
            credentials,
            attributes = new Dictionary<string, string[]>
            {
                ["name"] = new[] { name }
            }
        };
    }

    private async Task AssignOperatorRoleAsync(string token, string userId, CancellationToken ct)
    {
        var rolesRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/roles");
        rolesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rolesResponse = await _httpClient.SendAsync(rolesRequest, ct);
        rolesResponse.EnsureSuccessStatusCode();

        var roles = await rolesResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var operatorRole = roles.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("name").GetString() == "operator");

        if (operatorRole.ValueKind == JsonValueKind.Undefined)
        {
            _logger.LogWarning("Operator role not found in realm '{Realm}'", _options.Realm);
            return;
        }

        var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { operatorRole })
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var assignResponse = await _httpClient.SendAsync(assignRequest, ct);

        if (!assignResponse.IsSuccessStatusCode)
        {
            var errorBody = await assignResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to assign operator role to user {UserId}: {StatusCode} {Error}",
                userId, assignResponse.StatusCode, errorBody);
            throw new InvalidOperationException(
                $"Failed to assign operator role to Keycloak user '{userId}': {assignResponse.StatusCode}");
        }
    }

    public async Task<CreateOperatorResult> CreateOperatorAsync(
        string name, string email, string password, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var userId = await CreateKeycloakOperatorUserAsync(token, name, email, password, ct);

        await AssignOperatorRoleAsync(token, userId, ct);

        return new CreateOperatorResult(name, email, userId);
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

    public async Task<DisableOperatorResponse> DisableOperatorAsync(string email, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var searchRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users?email={Uri.EscapeDataString(email)}");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var searchResponse = await _httpClient.SendAsync(searchRequest, ct);

        if (!searchResponse.IsSuccessStatusCode)
        {
            var errorBody = await searchResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak user search failed: {StatusCode} {Error}",
                searchResponse.StatusCode, errorBody);
            searchResponse.EnsureSuccessStatusCode();
        }

        var users = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(ct);

        var user = users.EnumerateArray().FirstOrDefault();

        if (user.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"No user found with email '{email}'");
        }

        var userId = user.GetProperty("id").GetString()!;
        var wasAlreadyDisabled = !user.GetProperty("enabled").GetBoolean();

        var disablePayload = new { enabled = false };
        var disableRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}")
        {
            Content = JsonContent.Create(disablePayload)
        };
        disableRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var disableResponse = await _httpClient.SendAsync(disableRequest, ct);

        if (!disableResponse.IsSuccessStatusCode)
        {
            var errorBody = await disableResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak disable user failed: {StatusCode} {Error}",
                disableResponse.StatusCode, errorBody);
            disableResponse.EnsureSuccessStatusCode();
        }

        var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logoutResponse = await _httpClient.SendAsync(logoutRequest, ct);

        if (!logoutResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Keycloak logout failed (non-fatal): UserId={UserId}, StatusCode={StatusCode}",
                userId, logoutResponse.StatusCode);
        }

        var message = wasAlreadyDisabled
            ? "El operador ya estaba desactivado."
            : "Operador desactivado correctamente.";

        return new DisableOperatorResponse(message, wasAlreadyDisabled);
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
