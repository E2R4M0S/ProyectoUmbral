using Missions.Application.Operators.Create;
using Missions.Application.Operators.Disable;
using Missions.Application.Operators.Enable;
using Missions.Application.Users.GetUsers;

namespace Missions.Application.Common.Interfaces;

public interface IKeycloakAdminService
{
    Task<string> CreateUserAsync(string username, string email, string password, string firstName, string lastName, string? alias, CancellationToken ct);

    Task<CreateOperatorResult> CreateOperatorAsync(string name, string email, CancellationToken ct);

    Task ExecuteActionsEmailAsync(string userId, List<string> actions, CancellationToken ct);

    Task<DisableOperatorResponse> DisableOperatorAsync(string email, CancellationToken ct);

    Task<EnableOperatorResponse> EnableOperatorAsync(string email, CancellationToken ct);

    Task DeleteUserAsync(string userId, CancellationToken ct);

    Task UpdateUserAsync(string userId, string firstName, string lastName, string alias, CancellationToken ct);

    Task<IReadOnlyList<UserRepresentation>> GetUsersAsync(int first, int max, string? search, bool? enabled, CancellationToken ct);

    Task<IReadOnlyList<UserRepresentation>> GetUsersByRoleAsync(string role, int first, int max, CancellationToken ct);

    Task<IReadOnlyList<string>> GetUserRealmRolesAsync(string userId, CancellationToken ct);

    Task<UserRepresentation?> GetUserByIdAsync(string userId, CancellationToken ct);
}
