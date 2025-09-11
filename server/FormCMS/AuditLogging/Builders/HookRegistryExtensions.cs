using FormCMS.AuditLogging.Models;
using FormCMS.AuditLogging.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Utils.RecordExt;

namespace FormCMS.AuditLogging.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterAuditLogHooks(this HookRegistry registry)
    {
        registry.EntityPostAdd.RegisterDynamic("*",
            async (IAuditLogService service, EntityPostAddArgs args) =>
            {
                await service.AddLog(
                    ActionType.Create, 
                    args.Entity.Name, 
                    args.Record.StrOrEmpty(args.Entity.PrimaryKey),
                    args.Record.StrOrEmpty(args.Entity.LabelAttributeName),
                    args.Record) ;
                return args;
            }
        );
        registry.EntityPostDel.RegisterDynamic( "*",
            async (IAuditLogService service, EntityPostDelArgs args) =>
            {
                await service.AddLog(
                    ActionType.Delete, 
                    args.Entity.Name, 
                    args.Record.StrOrEmpty(args.Entity.PrimaryKey),
                    args.Record.StrOrEmpty(args.Entity.LabelAttributeName),
                    args.Record
                ) ;
                return args;
            }
        );
        registry.EntityPostUpdate.RegisterDynamic("*",
            async (IAuditLogService service, EntityPostUpdateArgs args) =>
            {
                await service.AddLog(
                    ActionType.Update,
                    args.Entity.Name,
                    args.Record.StrOrEmpty(args.Entity.PrimaryKey),
                    args.Record.StrOrEmpty(args.Entity.LabelAttributeName),
                    args.Record
                );
                return args;
            }
        );
    }
}