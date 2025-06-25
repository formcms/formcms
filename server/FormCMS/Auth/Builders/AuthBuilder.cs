using FluentResults;
using FormCMS.Auth.Handlers;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Core.Auth;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FormCMS.Auth.Builders;

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

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = ".AspNetCore.Identity.Application";
            });

        if (authConfig.GithubOAuthCredential is not null)
        {
            var github = authConfig.GithubOAuthCredential;
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

        if (authConfig.KeyAuthConfig is not null)
        {
            authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(CmsAuthSchemas.ApiKeyAuth, null);
        }

        services.AddAuthorization();

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
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.Services.GetService<PluginRegistry>()?.FeatureMenus.Add(AuthManageMenus.MenuRoles);
        app.Services.GetService<PluginRegistry>()?.FeatureMenus.Add(AuthManageMenus.MenuUsers);
        
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
