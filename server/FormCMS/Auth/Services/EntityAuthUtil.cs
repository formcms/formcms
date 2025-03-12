using FormCMS.Core.HookFactory;

namespace FormCMS.Auth.Services;
public static class EntityAuthUtil
{
    public static void RegisterHooks(HookRegistry registry)
    {
        registry.EntityPreGetSingle.RegisterDynamic("*", async (
            IEntityAuthService service,
            EntityPreGetSingleArgs args
        ) =>
        {
            await service.CheckGetSinglePermission(args.Entity, args.RecordId);
            return args;
        });

        registry.EntityPreGetList.RegisterDynamic("*", (
            IEntityAuthService service,
            EntityPreGetListArgs args
        ) =>
        {
            args = args with
            {
                RefFilters = service.ApplyListPermissionFilter(args.Name, args.Entity, args.RefFilters)
            };
            return args;
        });

        registry.JunctionPreAdd.RegisterDynamic("*", async (
            IEntityAuthService service,
            JunctionPreAddArgs args
        ) =>
        {
            await service.CheckUpdatePermission(args.Entity, args.RecordId);
            return args;
        });

        registry.JunctionPreDel.RegisterDynamic("*", async (
            IEntityAuthService service,
            JunctionPreDelArgs args
        ) =>
        {
            await service.CheckUpdatePermission(args.Entity, args.RecordId);
            return args;
        });

        registry.EntityPreDel.RegisterDynamic("*", async (
            IEntityAuthService service,
            EntityPreDelArgs args
        ) =>
        {
            await service.CheckUpdatePermission(args.Entity, args.RefRecord);
            return args;
        });

        registry.EntityPreUpdate.RegisterDynamic("*", async (
            IEntityAuthService service,
            EntityPreUpdateArgs args
        ) =>
        {
            await service.CheckUpdatePermission(args.Entity, args.RefRecord);
            return args;
        });

        registry.EntityPreAdd.RegisterDynamic("*", (
            IEntityAuthService service, EntityPreAddArgs args
        ) =>
        {
            service.CheckInsertPermission(args.Entity);
            service.AssignCreatedBy(args.RefRecord);
            return args;
        });
    }
}