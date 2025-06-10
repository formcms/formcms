using System.Security.Claims;
using FluentResults;
using FormCMS.Auth.Handlers;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FormCMS.Auth.Builders;

public record OAuthCredential(string ClientId, string ClientSecret);

public record OAuthConfig(OAuthCredential? Github);

public record AuthConfig(OAuthConfig? OAuthConfig = null, KeyAuthConfig? KeyAuthConfig = null);

public record KeyAuthConfig(string Key);

public sealed class AuthBuilder<TCmsUser>(ILogger<AuthBuilder<TCmsUser>> logger) : IAuthBuilder
    where TCmsUser : IdentityUser, new()
{
    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(
        IServiceCollection services,
        AuthConfig authConfig
    )
        where TUser : CmsUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        //add the builder itself, so the Web application knows if the feature is enabled
        services.AddSingleton(authConfig);
        services.AddSingleton<IAuthBuilder, AuthBuilder<TUser>>();

        services.AddIdentity<TUser, TRole>().AddEntityFrameworkStores<TContext>();

        services.AddHttpContextAccessor();

        if (authConfig.OAuthConfig is not null)
        {
            var oAuthConfig = authConfig.OAuthConfig;
            var authenticationBuilder = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".AspNetCore.Identity.Application";
                });
            if (oAuthConfig.Github is not null)
            {
                var github = oAuthConfig.Github;
                authenticationBuilder.AddGitHub(options =>
                {
                    options.ClientId = github.ClientId;
                    options.ClientSecret = github.ClientSecret;
                    options.Scope.Add("user:email");

                    options.Events.OnCreatingTicket = context =>
                        context
                            .HttpContext.RequestServices.GetRequiredService<ILoginService>()
                            .HandleGithubCallback(context);
                });
            }
        }
        if (authConfig.KeyAuthConfig is not null)
        {
            var authenticationBuilder = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".AspNetCore.Identity.Application";
                })
                .AddApiKey(
                    "ApiKey",
                    (
                        config =>
                        {
                            config.ApiKeyHeaderName = "X-Cms-Adm-Api-Key";
                        }
                    )
                );

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "CombinedSchemes";
                    options.DefaultChallengeScheme = "CombinedSchemes";
                })
                .AddPolicyScheme(
                    "CombinedSchemes",
                    "Cookie  or ApiKey",
                    options =>
                    {
                        options.ForwardDefaultSelector = context =>
                        {
                            // Check for API Key in header
                            if (context.Request.Headers.ContainsKey("X-Cms-Adm-Api-Key"))
                                return ApiKeyAuthenticationOptions.DefaultScheme;

                            return IdentityConstants.ApplicationScheme;
                        };
                    }
                );
        }

        services.AddScoped<IUserClaimsPrincipalFactory<CmsUser>, CustomPrincipalFactory>();
        services.AddScoped<ILoginService, LoginService<TUser>>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserManageService, UserManageService<TUser>>();
        services.AddScoped<IProfileService, ProfileService<TUser>>();
        services.AddScoped<IAccountService, AccountService<TUser, TRole, TContext>>();

        services.AddScoped<ISchemaAuthService, SchemaAuthService>();
        services.AddScoped<IEntityAuthService, EntityAuthService>();
        services.AddScoped<IAssetAuthService, AssetAuthService>();

        return services;
    }

    public WebApplication UseCmsAuth(WebApplication app)
    {
        Print();
        var authConfig = app.Services.GetRequiredService<AuthConfig>();
        if (authConfig.OAuthConfig is not null)
        {
            app.UseAuthentication();
        }

        app.Services.GetService<RestrictedFeatures>()
            ?.Menus.AddRange(AuthManageMenus.MenuRoles, AuthManageMenus.MenuUsers);

        MapEndpoints();
        RegisterHooks();
        return app;

        void MapEndpoints()
        {
            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapLoginHandlers();
            apiGroup.MapGroup("/accounts").MapAccountHandlers();
            apiGroup.MapGroup("/profile").MapProfileHandlers();
        }

        void RegisterHooks()
        {
            var registry = app.Services.GetRequiredService<HookRegistry>();
            SchemaAuthUtil.RegisterHooks(registry);
            EntityAuthUtil.RegisterHooks(registry);
            AssetAuthUtil.RegisterHooks(registry);
        }
    }

    public async Task<Result> EnsureCmsUser(
        WebApplication app,
        string email,
        string password,
        string[] role
    )
    {
        using var scope = app.Services.CreateScope();
        return await scope
            .ServiceProvider.GetRequiredService<IAccountService>()
            .EnsureUser(email, password, role);
    }

    private void Print()
    {
        logger.LogInformation(
            """
            *********************************************************
            Using CMS Auth API endpoints
            *********************************************************
            """
        );
    }
}
