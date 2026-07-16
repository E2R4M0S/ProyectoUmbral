using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Missions.Application.Common.Interfaces;
using Missions.Application.Operators.Create;
using Missions.Application.Operators.Disable;
using Missions.Application.Users.GetUsers;

namespace Missions.Infrastructure.Services;

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
        string username, string email, string password, string firstName, string lastName, string? alias, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var userId = await CreateKeycloakUserAsync(token, username, email, password, firstName, lastName, alias, ct);
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
        string token, string username, string email, string password, string firstName, string lastName, string? alias, CancellationToken ct)
    {
        var payload = BuildUserPayload(username, email, password, firstName, lastName, alias);

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

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var field = errorBody.Contains("username", StringComparison.OrdinalIgnoreCase)
                    ? "username"
                    : "email";
                var msg = field == "username"
                    ? $"ya está en uso."
                    : $"ya está registrado.";
                throw new Missions.Application.Common.Exceptions.RegistrationException(field, msg);
            }

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

    public async Task UpdateUserAsync(string userId, string firstName, string lastName, string alias, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var payload = new
        {
            firstName,
            lastName,
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

    public async Task<CreateOperatorResult> CreateOperatorAsync(
        string name, string email, string password, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);
        var userId = await CreateKeycloakOperatorUserAsync(token, name, email, ct);
        await AssignOperatorRoleAsync(token, userId, ct);
        return new CreateOperatorResult(name, email, userId);
    }

    private async Task<string> CreateKeycloakOperatorUserAsync(
        string token, string name, string email, CancellationToken ct)
    {
        var payload = BuildOperatorPayload(name, email);

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
                throw new InvalidOperationException($"Email '{email}' is already registered in Keycloak.");

            response.EnsureSuccessStatusCode();
        }

        var location = response.Headers.Location?.ToString();
        var userId = location?.Split('/').LastOrDefault();

        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("Failed to extract Keycloak user ID from Location header.");

        return userId;
    }

    private static object BuildOperatorPayload(string name, string email)
    {

        return new
        {
            username = email,
            email,
            emailVerified = true,
            enabled = true,
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

    public async Task ExecuteActionsEmailAsync(string userId, List<string> actions, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/execute-actions-email?lifespan=43200&redirect_uri={Uri.EscapeDataString($"http://localhost:5173")}&client_id=umbral-frontend")
        {
            Content = JsonContent.Create(actions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Keycloak execute-actions-email failed for user {UserId} with actions {Actions}: {StatusCode} {Error}",
                userId, string.Join(", ", actions), response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation(
            "execute-actions-email dispatched to user {UserId} for actions: {Actions}",
            userId, string.Join(", ", actions));
    }

    public async Task<CreateOperatorResult> CreateOperatorAsync(
        string name, string email, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var userId = await CreateKeycloakOperatorUserAsync(token, name, email, ct);

        await AssignOperatorRoleAsync(token, userId, ct);

        await ExecuteActionsEmailAsync(userId, ["UPDATE_PASSWORD"], ct);

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
            throw new InvalidOperationException($"No user found with email '{email}'");

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

    private static object BuildUserPayload(string username, string email, string password, string firstName, string lastName, string? alias)
    {
        var credentials = new[]
        {
            new { type = "password", value = password, temporary = false }
        };

        var payload = new Dictionary<string, object>
        {
            ["username"] = username,
            ["firstName"] = firstName,
            ["lastName"] = lastName,
            ["email"] = email,
            ["emailVerified"] = false,
            ["enabled"] = true,
            ["credentials"] = credentials
        };

        if (alias is not null)
        {
            payload["attributes"] = new Dictionary<string, string[]>
            {
                ["alias"] = new[] { alias }
            };
        }

        return payload;
    }

    public async Task<IReadOnlyList<UserRepresentation>> GetUsersAsync(
        int first, int max, string? search, bool? enabled, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var queryParams = new List<string> { $"first={first}", $"max={max}" };
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (enabled.HasValue)
            queryParams.Add($"enabled={enabled.Value.ToString().ToLowerInvariant()}");

        var url = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users?{string.Join("&", queryParams)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Keycloak get users failed: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var result = new List<UserRepresentation>();
        foreach (var element in json.EnumerateArray())
            result.Add(ParseUserRepresentation(element));

        return result;
    }

    public async Task<IReadOnlyList<UserRepresentation>> GetUsersByRoleAsync(
        string role, int first, int max, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var url = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/roles/{Uri.EscapeDataString(role)}/users?first={first}&max={max}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Keycloak get users by role failed: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var result = new List<UserRepresentation>();
        foreach (var element in json.EnumerateArray())
            result.Add(ParseUserRepresentation(element));

        return result;
    }

    public async Task<IReadOnlyList<string>> GetUserRealmRolesAsync(string userId, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var url = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Keycloak get user realm roles failed: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var roles = new List<string>();
        foreach (var element in json.EnumerateArray())
        {
            if (element.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                if (!string.IsNullOrEmpty(name))
                    roles.Add(name);
            }
        }

        return roles;
    }

    public async Task<UserRepresentation?> GetUserByIdAsync(string userId, CancellationToken ct)
    {
        var token = await GetAdminTokenAsync(ct);

        var url = $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{userId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Keycloak get user by id failed: {StatusCode} {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return ParseUserRepresentation(json);
    }

    private static UserRepresentation ParseUserRepresentation(JsonElement element)
    {
        var rep = new UserRepresentation
        {
            Id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
            FirstName = element.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() : null,
            Email = element.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : "",
            Enabled = element.TryGetProperty("enabled", out var enProp) && enProp.GetBoolean(),
            CreatedTimestamp = element.TryGetProperty("createdTimestamp", out var ctProp) ? ctProp.GetInt64() : 0,
            EmailVerified = element.TryGetProperty("emailVerified", out var evProp) && evProp.GetBoolean()
        };

        if (element.TryGetProperty("attributes", out var attrsProp) && attrsProp.ValueKind == JsonValueKind.Object)
        {
            var dict = ImmutableDictionary.CreateBuilder<string, string[]>();
            foreach (var prop in attrsProp.EnumerateObject())
            {
                var values = prop.Value.ValueKind == JsonValueKind.Array
                    ? prop.Value.EnumerateArray().Select(v => v.GetString() ?? "").ToArray()
                    : Array.Empty<string>();
                dict.Add(prop.Name, values);
            }
            rep.Attributes = dict.ToImmutable();
        }

        return rep;
    }
}
