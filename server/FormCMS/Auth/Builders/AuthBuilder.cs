using FormCMS.Auth.Handlers;
using FluentResults;
using FormCMS.Auth.DTO;
using FormCMS.Auth.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Utils.IdentityExt;
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

        var systemResources = new SystemResources(Menus:
            [SystemMenus.MenuUsers, SystemMenus.MenuRoles, SystemMenus.MenuSchemaBuilder]);
        services.AddSingleton(systemResources);
        services.AddSingleton<IAuthBuilder, AuthBuilder<TUser>>();
        
        services.AddHttpContextAccessor();
        services.AddIdentityApiEndpoints<TUser>()
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
        
        services.AddScoped<IAccountService, AccountService<TUser, TRole, TContext>>();
        services.AddScoped<ISchemaPermissionService, SchemaPermissionService<TUser>>();
        services.AddScoped<IEntityPermissionService, EntityPermissionService>();
        services.AddScoped<IProfileService, ProfileService<TUser>>();
        
        return services;
    }

    public WebApplication UseCmsAuth(WebApplication app)
    {
        Print();
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

            registry.SchemaPreSave.RegisterDynamic("*", async (
                IHttpContextAccessor accessor,  ISchemaPermissionService schemaPermissionService, SchemaPreSaveArgs args
            ) =>
            {
                accessor.HttpContext.SaveIdentityToItems();
                return args with
                {
                    RefSchema = await schemaPermissionService.BeforeSave(args.RefSchema)
                };
            });

            registry.SchemaPostSave.RegisterDynamic("*", async (
                IHttpContextAccessor accessor,  ISchemaPermissionService schemaPermissionService, SchemaPostSaveArgs args
            ) =>
            {
                await schemaPermissionService.AfterSave(args.Schema);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.SchemaPreDel.RegisterDynamic("*", async (
                IHttpContextAccessor accessor,  ISchemaPermissionService schemaPermissionService, SchemaPreDelArgs args
            ) =>
            {
                await schemaPermissionService.Delete(args.SchemaId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.SchemaPreGetAll.RegisterDynamic("*", (
                IHttpContextAccessor accessor, ISchemaPermissionService schemaPermissionService, SchemaPreGetAllArgs args
            ) =>
            {
                accessor.HttpContext.SaveIdentityToItems();
                return args with { OutSchemaNames = schemaPermissionService.GetAll() };
            });

            registry.SchemaPostGetSingle.RegisterDynamic("*", (
                IHttpContextAccessor accessor,  ISchemaPermissionService schemaPermissionService, SchemaPostGetSingleArgs args
            ) =>
            {
                schemaPermissionService.GetOne(args.Schema);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.EntityPreGetSingle.RegisterDynamic("*", (
               IHttpContextAccessor accessor,   IEntityPermissionService service, EntityPreGetSingleArgs args
            ) =>
            {
                service.GetOne(args.Name, args.RecordId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.EntityPreGetList.RegisterDynamic("*", (
               IHttpContextAccessor accessor,  IEntityPermissionService service, EntityPreGetListArgs args
            ) =>
            {
                
                args = args with { RefFilters = service.List(args.Name, args.Entity, args.RefFilters) };
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.JunctionPreAdd.RegisterDynamic("*", async (
                IHttpContextAccessor accessor, IEntityPermissionService service, JunctionPreAddArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.JunctionPreDel.RegisterDynamic("*", async (
                IHttpContextAccessor accessor, IEntityPermissionService service, JunctionPreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.EntityPreDel.RegisterDynamic("*", async (
                IHttpContextAccessor accessor, IEntityPermissionService service, EntityPreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.EntityPreUpdate.RegisterDynamic("*", async (
                IHttpContextAccessor accessor, IEntityPermissionService service, EntityPreUpdateArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });

            registry.EntityPreAdd.RegisterDynamic("*", (
                IHttpContextAccessor accessor, IEntityPermissionService service, EntityPreAddArgs args
            ) =>
            {
                service.Create(args.Name);
                service.AssignCreatedBy(args.RefRecord);
                accessor.HttpContext.SaveIdentityToItems();
                return args;
            });
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