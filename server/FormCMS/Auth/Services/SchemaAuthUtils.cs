using FormCMS.Core.HookFactory;

namespace FormCMS.Auth.Services;
public static class SchemaAuthUtil
{
    public static void RegisterHooks(HookRegistry registry)
    {
        registry.SchemaPreSave.RegisterDynamic("*", async (
            ISchemaAuthService service,
            SchemaPreSaveArgs args
        ) => args with
        {
            RefSchema = await service.BeforeSave(args.RefSchema)
        });

        registry.SchemaPostSave.RegisterDynamic("*", async (
            ISchemaAuthService service, SchemaPostSaveArgs args
        ) =>
        {
            await service.AfterSave(args.Schema);
            return args;
        });

        registry.SchemaPreDel.RegisterDynamic("*", async (
            ISchemaAuthService service, SchemaPreDelArgs args
        ) =>
        {
            await service.Delete(args.Schema);
            return args;
        });

        registry.SchemaPreGetAll.RegisterDynamic("*", (
            ISchemaAuthService service,
            SchemaPreGetAllArgs args
        ) =>
        {
            service.CheckRole();
            return args;
        });

        registry.SchemaPostGetSingle.RegisterDynamic("*", (
            ISchemaAuthService service, SchemaPostGetSingleArgs args
        ) =>
        {
            service.CheckRole();
            return args;
        });
    }
}
