using FormCMS.Auth.Builders;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FormCMS.Auth.Handlers;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiKeyAuthenticationOptions _options;
    private readonly AuthConfig _authConfig;
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,AuthConfig authConfig)
        : base(options, logger, encoder, clock)
    {
        _options = options.CurrentValue;
        _authConfig = authConfig??throw new InvalidOperationException("Missing AuthConfig ");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(nameof(_authConfig.KeyAuthConfig));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey))
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

      
        if (providedApiKey !=_authConfig.KeyAuthConfig!.Key)
        {
            return AuthenticateResult.Fail("Unauthorized client");
        }

        // Invoke event
        var context = new ApiKeyValidatedContext(Context, Scheme, Options, providedApiKey);
        await Options.OnApiKeyValidated(context);

        if (context.Result != null)
        {
            return context.Result;
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "sadmin@cms.com"),new Claim(ClaimTypes.Role,Roles.Sa) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

}


