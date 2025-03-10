using FormCMS.Auth.Handlers;
using FluentResults;
using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Identities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FormCMS.Auth.Builders;

public sealed class AuthBuilder<TCmsUser> (ILogger<AuthBuilder<TCmsUser>> logger): IAuthBuilder
    where TCmsUser : IdentityUser, new()
{
    public static IServiceCollection AddCmsAuth<TUser, TRole, TContext>(IServiceCollection services)
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityDbContext<TUser>
    {
        services.AddSingleton<IAuthBuilder, AuthBuilder<TUser>>();
        
        services.AddHttpContextAccessor();
        services.AddIdentityApiEndpoints<TUser>()
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
        
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
            apiGroup.MapGet("/logout", async (
                SignInManager<TCmsUser> signInManager
            ) => await signInManager.SignOutAsync());
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