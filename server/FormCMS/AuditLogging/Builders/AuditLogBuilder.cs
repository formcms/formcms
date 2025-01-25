using FormCMS.AuditLogging.Models;
using FormCMS.AuditLogging.Services;
using FormCMS.Core.HookFactory;

namespace FormCMS.AuditLogging.Builders;

public sealed class AuditLogBuilder(ILogger<AuditLogBuilder> logger )
{
    public static IServiceCollection AddAuditLog(IServiceCollection services)
    {
        services.AddSingleton<AuditLogBuilder>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services;
    }

    public async Task<WebApplication> UseAuditLog(WebApplication app)
    {
        logger.LogInformation(
            """
            *********************************************************
            Using AuditLog
            *********************************************************
            """);

        using var scope = app.Services.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        await auditLogService.EnsureAuditLogTable();
        
        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.EntityPostAdd.RegisterDynamic("*",
            async (IAuditLogService service, EntityPostAddArgs args) =>
            {
                await service.AddLog(ActionType.Create, args.Name, args.RecordId, args.Record) ;
                return args;
            }
        );
        registry.EntityPostDel.RegisterDynamic( "*",
            async (IAuditLogService service, EntityPostDelArgs args) =>
            {
                await service.AddLog(ActionType.Delete, args.Name, args.RecordId, args.Record);
                return args;
            }
        );
        registry.EntityPostUpdate.RegisterDynamic("*",
            async (IAuditLogService service, EntityPostAddArgs args) =>
            {
                await service.AddLog(ActionType.Update, args.Name, args.RecordId, args.Record);
                return args;
            }
        );
        return app;
    }

}