using FormCMS.Auth.Handlers;
using FormCMS.Cms.Builders;
using FluentResults;
using FormCMS.Auth.Services;
using FormCMS.Cms;
using FormCMS.Core.HookFactory;
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
                ISchemaPermissionService schemaPermissionService, SchemaPreSaveArgs args
            ) => args with
            {
                RefSchema = await schemaPermissionService.BeforeSave(args.RefSchema)
            });
            
            registry.SchemaPostSave.RegisterDynamic("*", async (
                ISchemaPermissionService schemaPermissionService, SchemaPostSaveArgs args
            ) =>
            {
                await schemaPermissionService.AfterSave(args.Schema);
                return args;
            });

            registry.SchemaPreDel.RegisterDynamic("*", async (
                ISchemaPermissionService schemaPermissionService, SchemaPreDelArgs args
            ) =>
            {
                await schemaPermissionService.Delete(args.SchemaId);
                return args;
            });

            registry.SchemaPreGetAll.RegisterDynamic("*", (
                ISchemaPermissionService schemaPermissionService, SchemaPreGetAllArgs args
            ) => args with { OutSchemaNames = schemaPermissionService.GetAll() });

            registry.SchemaPostGetSingle.RegisterDynamic("*", (
                ISchemaPermissionService schemaPermissionService, SchemaPostGetSingleArgs args
            ) =>
            {
                schemaPermissionService.GetOne(args.Schema);
                return args;
            });

            registry.EntityPreGetSingle.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreGetSingleArgs args
            ) =>
            {
                service.GetOne(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreGetList.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreGetListArgs args
            ) => args with { RefFilters = service.List(args.Name, args.Entity, args.RefFilters) });

            registry.JunctionPreAdd.RegisterDynamic("*", async (
                IEntityPermissionService service, JunctionPreAddArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.JunctionPreDel.RegisterDynamic("*", async (
                IEntityPermissionService service, JunctionPreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreDel.RegisterDynamic("*", async (
                IEntityPermissionService service, EntityPreDelArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreUpdate.RegisterDynamic("*", async (
                IEntityPermissionService service, EntityPreUpdateArgs args
            ) =>
            {
                await service.Change(args.Name, args.RecordId);
                return args;
            });

            registry.EntityPreAdd.RegisterDynamic("*", (
                IEntityPermissionService service, EntityPreAddArgs args
            ) =>
            {
                service.Create(args.Name);
                service.AssignCreatedBy(args.RefRecord);
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