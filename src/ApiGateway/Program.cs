using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// JWT Bearer authentication — JWKS cached keyset via ConfigurationManager
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        // ConfigurationManager auto-caches the JWKS with default 5-min refresh
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
    });

// Transform Keycloak realm_access.roles into ClaimTypes.Role claims
builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesTransformer>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));
    options.AddPolicy("operator", policy =>
        policy.RequireRole("operator"));
    options.AddPolicy("participant", policy =>
        policy.RequireRole("participant"));
    options.AddPolicy("operator_or_participant", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("operator") || ctx.User.IsInRole("participant")));
    options.AddPolicy("operator_or_admin", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
});

// YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Public health endpoint (no auth)
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "ApiGateway" }))
   .AllowAnonymous();

// YARP middleware
app.MapReverseProxy();

app.Run();

