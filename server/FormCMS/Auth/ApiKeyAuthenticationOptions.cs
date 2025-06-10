using FormCMS.Auth.Services;
using Microsoft.AspNetCore.Authentication;

namespace FormCMS.Auth
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";
        public string Scheme => DefaultScheme;
        public string ApiKeyHeaderName { get; set; } = "X-ADM-API-KEY";

        public Func<ApiKeyValidatedContext, Task> OnApiKeyValidated { get; set; } = context => Task.CompletedTask;
    }
}
