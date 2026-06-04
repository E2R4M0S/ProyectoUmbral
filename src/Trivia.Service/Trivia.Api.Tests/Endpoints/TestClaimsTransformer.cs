using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Trivia.Api.Tests.Endpoints;

public class TestClaimsTransformer : IClaimsTransformation
{
    private readonly string _role;

    public TestClaimsTransformer(string role) => _role = role;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Role, _role));
        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
