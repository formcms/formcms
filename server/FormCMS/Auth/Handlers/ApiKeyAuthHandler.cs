using FormCMS.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FormCMS.Auth.Handlers;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthConfig _authConfig;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthConfig authConfig
    )
        : base(options, logger, encoder)
    {
        _authConfig = authConfig;
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(_authConfig.KeyAuthConfig));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(KeyAuthConstants.ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey))
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }


        if (providedApiKey != _authConfig.KeyAuthConfig!.Key)
        {
            return AuthenticateResult.Fail("Unauthorized client");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, KeyAuthConstants.ApiKeyUser),
            new Claim(ClaimTypes.Role, Roles.Sa)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}