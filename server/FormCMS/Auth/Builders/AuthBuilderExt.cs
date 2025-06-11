using AspNet.Security.OAuth.GitHub;
using FormCMS.Auth.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NJsonSchema.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace FormCMS.Auth;

    public  static class AuthBuilderExt
    {

    public static AuthenticationBuilder AddApiKey(
          this AuthenticationBuilder builder,
          string scheme,
          Action<ApiKeyAuthenticationOptions> configureOptions)
    {
        // THIS is where the handler gets registered
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            scheme,
            displayName: null,
            configureOptions);
    }

}

       
       

    

