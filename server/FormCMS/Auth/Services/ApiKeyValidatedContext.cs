using Microsoft.AspNetCore.Authentication;

namespace FormCMS.Auth.Services
{
    public class ApiKeyValidatedContext : ResultContext<ApiKeyAuthenticationOptions>
    {
        public string ApiKey { get; }

        public ApiKeyValidatedContext(
            HttpContext context,
            AuthenticationScheme scheme,
            ApiKeyAuthenticationOptions options,
            string apiKey)
            : base(context, scheme, options)
        {
            ApiKey = apiKey;
        }
    }
}
