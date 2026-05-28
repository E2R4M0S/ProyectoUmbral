namespace Teams.Application.Common.Interfaces;

public interface IKeycloakAdminService
{
    Task<string> CreateUserAsync(string username, string email, string password, string? alias, CancellationToken ct);

    Task DeleteUserAsync(string userId, CancellationToken ct);

    Task UpdateUserAsync(string userId, string name, string alias, CancellationToken ct);
}
