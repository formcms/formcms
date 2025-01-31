using FluentResults;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public sealed class SchemaService(
    DatabaseMigrator migrator,
    KateQueryExecutor queryExecutor,
    HookRegistry hook,
    IServiceProvider provider
) : ISchemaService
{
    public async Task<Schema[]> AllWithAction(SchemaType? type, CancellationToken ct = default)
    {
        var res = await hook.SchemaPreGetAll.Trigger(provider, new SchemaPreGetAllArgs());
        return await All(type, null, ct);
    }

    public async Task<Schema[]> All(SchemaType? type, IEnumerable<string>? names,
        CancellationToken ct = default)
    {
        var items = await queryExecutor.Many(SchemaHelper.ByNameAndType(type,names), ct);
        return items.Select(x => SchemaHelper.RecordToSchema(x).Ok()).ToArray();
    }

    public async Task<Schema?> ByIdWithAction(int id, CancellationToken ct = default)
    {
        var schema = await ById(id, ct);
        if (schema is not null)
        {
            await hook.SchemaPostGetSingle.Trigger(provider, new SchemaPostGetSingleArgs(schema));
        }

        return schema;

    }

    public async Task<Schema?> ById(int id, CancellationToken ct = default)
    {
        var item = await queryExecutor.Single(SchemaHelper.ById(id), ct);
        var res = SchemaHelper.RecordToSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNameDefault(string name, SchemaType type, CancellationToken ct = default)
    {
        var item = await queryExecutor.Single(SchemaHelper.ByNameAndType(type,[name]), ct);
        var res = SchemaHelper.RecordToSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> GetByNamePrefixDefault(string name, SchemaType type,
        CancellationToken ct = default)
    {
        var item = await queryExecutor.Single(SchemaHelper.ByStartsNameAndType(name,type), ct);

        var res = SchemaHelper.RecordToSchema(item);

        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema> SaveWithAction(Schema schema, CancellationToken ct)
    {
        await NameNotTakenByOther(schema, ct).Ok();
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        schema = await Save(schema, ct);
        await hook.SchemaPostSave.Trigger(provider, new SchemaPostSaveArgs(schema));
        return schema;
    }

    public async Task<Schema> AddOrUpdateByNameWithAction(Schema schema, CancellationToken ct)
    {
        var find = await GetByNameDefault(schema.Name, schema.Type, ct);
        if (find is not null)
        {
            schema = schema with { Id = find.Id };
        }

        var res = await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema));
        return await Save(res.RefSchema, ct);
    }

    public async Task Delete(int id, CancellationToken ct)
    {
        var schema = await ById(id,ct)?? throw new ResultException($"Schema [{id}] not found");
        await hook.SchemaPreDel.Trigger(provider, new SchemaPreDelArgs(schema));
        await queryExecutor.ExecInt(SchemaHelper.SoftDelete(id), ct);
    }

    public async Task EnsureTopMenuBar(CancellationToken ct)
    {
        var item = await queryExecutor.Single(SchemaHelper.ByNameAndType(SchemaType.Menu, [SchemaName.TopMenuBar]), ct);
        if (item is not null)
        {
            return;
        }

        var menuBarSchema = new Schema
        (
            Name: SchemaName.TopMenuBar,
            Type: SchemaType.Menu,
            Settings: new Settings
            (
                Menu: new Menu
                (
                    Name: SchemaName.TopMenuBar,
                    MenuItems: []
                )
            )
        );

        await Save(menuBarSchema, ct);
    }

    public Task EnsureSchemaTable() => migrator.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);

    public async Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await GetByNameDefault(SchemaName.TopMenuBar, SchemaType.Menu, ct) ??
                            throw new ResultException("Cannot find top menu bar");
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is null)
        {
            return;
        }

        var link = "/entities/" + entity.Name;
        var menus = menuBar.MenuItems.Where(x => x.Url != link);
        menuBar = menuBar with { MenuItems = [..menus] };
        menuBarSchema = menuBarSchema with { Settings = new Settings(Menu: menuBar) };
        await Save(menuBarSchema, ct);
    }


    public async Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await GetByNameDefault(SchemaName.TopMenuBar, SchemaType.Menu, ct) ??
                            throw new ResultException("cannot find top menu bar");
        var menuBar = menuBarSchema.Settings.Menu;
        if (menuBar is not null)
        {
            var link = "/entities/" + entity.Name;
            var menuItem = menuBar.MenuItems.FirstOrDefault(me => me.Url.StartsWith(link));
            if (menuItem is null)
            {
                menuBar = menuBar with
                {
                    MenuItems =
                    [
                        ..menuBar.MenuItems, new MenuItem(Icon: "pi-bolt", Url: link, Label: entity.DisplayName)
                    ]
                };
            }

            menuBarSchema = menuBarSchema with { Settings = new Settings(Menu: menuBar) };
            await Save(menuBarSchema, ct);
        }
    }

    public async Task<Schema> Save(Schema dto, CancellationToken ct)
    {
        var id = await queryExecutor.ExecInt(dto.Save(), ct);
        if (dto.Id == 0)
        {
            dto = dto with { Id = id };
        }

        return dto;
    }

    public async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct)
    {
        var count = await queryExecutor.Count(SchemaHelper.ByNameAndTypeAndNotId(schema.Name,schema.Type,schema.Id), ct);
        return count == 0 ? Result.Ok() : Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }
}
