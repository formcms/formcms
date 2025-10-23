using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.AuditLogging.Handlers;
using FormCMS.AuditLogging.Models;
using FormCMS.AuditLogging.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.RecordExt;

namespace FormCMS.AuditLogging.Builders;

public sealed class AuditLogBuilder(ILogger<AuditLogBuilder> logger )
{
    public static IServiceCollection AddAuditLog(IServiceCollection services)
    {
        services.AddSingleton<AuditLogBuilder>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.ConfigureHttpJsonOptions(AddCamelEnumConverter<ActionType>);

        return services;
    }

    private static void AddCamelEnumConverter<T>(Microsoft.AspNetCore.Http.Json.JsonOptions options) where T : struct, Enum
        => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase));
    public async Task<WebApplication> UseAuditLog(WebApplication app)
    {
        var options = app.Services.GetRequiredService<SystemSettings>();
        var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
        apiGroup.MapGroup("/audit_log").MapAuditLogHandlers();
        
        app.Services.GetRequiredService<HookRegistry>().RegisterAuditLogHooks();
        app.Services.GetRequiredService<PluginRegistry>().RegisterAuditLogPlugins();
        
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao.EnsureAuditLogTables();
        
        logger.LogInformation(
            """
            *********************************************************
            Using AuditLog
            *********************************************************
            """);
        return app;
    }

}