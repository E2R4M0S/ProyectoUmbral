using Teams.Application.Teams.Operators.Create;
using Teams.Application.Teams.Operators.Disable;

namespace Teams.Application.Common.Interfaces;

public interface IKeycloakAdminService
{
    Task<string> CreateUserAsync(string username, string email, string password, string? alias, CancellationToken ct);

    Task<CreateOperatorResult> CreateOperatorAsync(string name, string email, string password, CancellationToken ct);

    Task<DisableOperatorResponse> DisableOperatorAsync(string email, CancellationToken ct);

    Task DeleteUserAsync(string userId, CancellationToken ct);
}
