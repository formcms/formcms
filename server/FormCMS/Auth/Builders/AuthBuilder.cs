using System.Security.Claims;
using FormCMS.Auth.Handlers;
using FluentResults;
using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FormCMS.Auth.Builders;

public record OAuthCredential(string ClientId, string ClientSecret);
public record OAuthConfig(OAuthCredential? Github);
public record AuthConfig(OAuthConfig? OAuthConfig = null);

public sealed class AuthBuilder<TCmsUser> (ILogger<AuthBuilder<TCmsUser>> logger): IAuthBuilder
    where TCmsUser : IdentityUser, new()
{
    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(
        IServiceCollection services,
        AuthConfig authConfig 
        )
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        //add the builder itself, so the Web application knows if the feature is enabled
        services.AddSingleton(authConfig);
        services.AddSingleton<IAuthBuilder, AuthBuilder<TUser>>();
        
        services.AddHttpContextAccessor();
        services.AddIdentityApiEndpoints<TUser>()
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();

        if (authConfig.OAuthConfig is not null)
        {
            var oAuthConfig  = authConfig.OAuthConfig;
            var authenticationBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                })
                
                .AddCookie(options=>
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

                    options.Events.OnCreatingTicket = async context =>
                    {
                        var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value
                                    ?? context.User.GetProperty("email").GetString();

                        if (string.IsNullOrEmpty(email))
                        {
                            throw new Exception("Email not found from GitHub.");
                        }

                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<IdentityUser>>();
                        var signInManager = context.HttpContext.RequestServices
                            .GetRequiredService<SignInManager<IdentityUser>>();

                        var user = await userManager.FindByEmailAsync(email);
                        if (user == null)
                        {
                            user = new IdentityUser { UserName = email, Email = email };
                            await userManager.CreateAsync(user);
                        }

                        await signInManager.SignInAsync(user, isPersistent: false);
                    };
                });
            }
        }


        services.AddScoped<ILoginService, LoginService<TUser>>();
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

        app.Services.GetService<RestrictedFeatures>()?.Menus.AddRange(AuthMenus.MenuRoles,AuthMenus.MenuUsers);
        
        MapEndpoints();
        RegisterHooks();
        return app;

        void MapEndpoints()
        {
            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapIdentityApi<TCmsUser>();
            apiGroup.MapGroup("/accounts").MapAccountHandlers();
            apiGroup.MapLoginHandlers();
        }

        void RegisterHooks()
        {
            var registry = app.Services.GetRequiredService<HookRegistry>();
            SchemaAuthUtil.RegisterHooks(registry);
            EntityAuthUtil.RegisterHooks(registry);
            AssetAuthUtil.RegisterHooks(registry);
        }
    }

    public async Task<Result> EnsureCmsUser(WebApplication app, string email, string password, string[] role)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAccountService>().EnsureUser(email, password,role);
    }



    private void Print()
    {
        logger.LogInformation(
            """
            *********************************************************
            Using CMS Auth API endpoints
            *********************************************************
            """);
    }
}