namespace Missions.Infrastructure;

public class KeycloakAdminOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Realm { get; set; } = "umbral";
    // Override público para X-Forwarded-Host en action tokens (ej: URL del Cloudflare Tunnel)
    public string? PublicForwardedHost { get; set; }
}
