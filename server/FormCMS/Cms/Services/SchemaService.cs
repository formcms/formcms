using FluentResults;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using Humanizer;

namespace FormCMS.Cms.Services;

public sealed class SchemaService(
    DatabaseMigrator migrator,
    KateQueryExecutor queryExecutor,
    HookRegistry hook,
    IServiceProvider provider
) : ISchemaService
{
    public async Task<Schema[]> AllWithAction(SchemaType? type, PublicationStatus?status, CancellationToken ct = default)
    {
        await hook.SchemaPreGetAll.Trigger(provider, new SchemaPreGetAllArgs());
        return await All(type, null, status,ct);
    }

    public Task Publish(Schema schema, CancellationToken ct = default)
        =>queryExecutor.ExecBatch([
            (schema.ResetPublished(),false),
            (schema.Publish(),false)
        ], ct);

    public async Task<Schema[]> All(SchemaType? type, IEnumerable<string>? names, PublicationStatus? status, CancellationToken ct = default)
    {
        var query = SchemaHelper.ByNameAndType(type, names, status);
        var items = await queryExecutor.Many(query, ct);
        return items.Select(x => SchemaHelper.RecordToSchema(x).Ok()).ToArray();
    }

    public async Task<Schema?> ByIdWithAction(long id, CancellationToken ct = default)
    {
        var schema = await ById(id, ct);
        if (schema is not null)
        {
            await hook.SchemaPostGetSingle.Trigger(provider, new SchemaPostGetSingleArgs(schema));
        }
        return schema;
    }
    
    public async Task<Schema[]> History(string schemaId, CancellationToken ct = default)
    {
        var query = SchemaHelper.BySchemaId(schemaId);
        var items = await queryExecutor.Many(query, ct);
        var schemas= items.Select(x => SchemaHelper.RecordToSchema(x).Ok()).ToArray();
        await hook.SchemaPostGetHistory.Trigger(provider, new SchemaPostGetHistoryArgs([..schemas]));
        return schemas;
    }

    public async Task<Schema?> ById(long id, CancellationToken ct = default)
    {
        var query = SchemaHelper.ById(id);
        var item = await queryExecutor.Single(query, ct);
        var res = SchemaHelper.RecordToSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> ByNameOrDefault(string name, SchemaType type, PublicationStatus?status, CancellationToken ct = default)
    {
        var query = SchemaHelper.ByNameAndType(type, [name],status);
        var item = await queryExecutor.Single(query, ct);
        var res = SchemaHelper.RecordToSchema(item);
        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Schema?> ByStartsOrDefault(string name, SchemaType type, PublicationStatus?status,
        CancellationToken ct = default)
    {
        var item = await queryExecutor.Single(SchemaHelper.StartsAndType(name,type,status), ct);

        var res = SchemaHelper.RecordToSchema(item);

        return res.IsSuccess ? res.Value : null;
    }

    public async Task<Result> NameNotTakenByOther(Schema schema, CancellationToken ct)
    {
        var query = SchemaHelper.ByNameAndTypeAndNotId(schema.Name, schema.Type, schema.SchemaId);
        var count = await queryExecutor.Count(query, ct);
        return count == 0 ? Result.Ok() : Result.Fail($"the schema name {schema.Name} was taken by other schema");
    }

    public async Task<Schema> Save(Schema schema, bool asPublished, CancellationToken ct)
    {
        schema = schema.Init(asPublished);
        var queries = new List<(SqlKata.Query, bool)>
        {
            (schema.ResetLatest(),false),
        };
        if (schema.PublicationStatus == PublicationStatus.Published)
        {
            queries.Add((schema.ResetPublished(), false));
        }
        queries.Add((schema.Save(), true));

        var ids = await queryExecutor.ExecBatch([..queries], ct);
        schema = schema with { Id = ids.Last()};
        return schema;
    }
    

    public async Task<Schema> SaveWithAction(Schema schema, bool asPublished, CancellationToken ct)
    {
        schema = schema with
        {
            Name = schema.Type switch
            {
                SchemaType.Entity => schema.Settings.Entity!.Name,
                SchemaType.Query => schema.Settings.Query!.Name,
                SchemaType.Menu => schema.Settings.Menu!.Name,
                SchemaType.Page => schema.Settings.Page!.Name,
                _ => schema.Name
            }
        };
        
        await NameNotTakenByOther(schema, ct).Ok();
        schema = (await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema))).RefSchema;
        schema = await Save(schema, asPublished, ct);
        await hook.SchemaPostSave.Trigger(provider, new SchemaPostSaveArgs(schema));
        return schema;
    }

    public async Task<Schema> AddOrUpdateByNameWithAction(Schema schema,bool asPublished, CancellationToken ct)
    {
        var find = await ByNameOrDefault(schema.Name, schema.Type, null, ct);
        if (find is not null)
        {
            schema = schema with { SchemaId = find.SchemaId};
        }

        var res = await hook.SchemaPreSave.Trigger(provider, new SchemaPreSaveArgs(schema));
        return await Save(res.RefSchema, asPublished,ct);
    }

    public async Task Delete(long id, CancellationToken ct)
    {
        var schema = await ById(id,ct)?? throw new ResultException($"Schema [{id}] not found");
        await hook.SchemaPreDel.Trigger(provider, new SchemaPreDelArgs(schema));
        await queryExecutor.Exec(SchemaHelper.SoftDelete(schema.SchemaId),false, ct);
    }

    public async Task EnsureTopMenuBar(CancellationToken ct)
    {
        var query = SchemaHelper.ByNameAndType(SchemaType.Menu, [SchemaName.TopMenuBar], null);
        var item = await queryExecutor.Single(query, ct);
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

        await Save(menuBarSchema, true, ct);
    }

    public Task EnsureSchemaTable() => migrator.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);

    public async Task RemoveEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await ByNameOrDefault(SchemaName.TopMenuBar, SchemaType.Menu, null,ct) ??
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
        await Save(menuBarSchema,true, ct);
    }


    public async Task EnsureEntityInTopMenuBar(Entity entity, CancellationToken ct)
    {
        var menuBarSchema = await ByNameOrDefault(SchemaName.TopMenuBar, SchemaType.Menu,null, ct) ??
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
            await Save(menuBarSchema,true, ct);
        }
    }

   
}
