using System.Text.Json;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.HttpClientExt;
using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.CoreKit.ApiClient;

public class SchemaApiClient (HttpClient client)
{
    
    //for admin panel
    public Task<Result<Menu>> GetTopMenuBar() => client.GetResult<Menu>("/menu/top-menu-bar".ToSchemaApi());
    
    public Task<Result<XEntity>> GetLoadedEntity(string entityName)
        => client.GetResult<XEntity>($"/entity/{entityName}".ToSchemaApi());
    
    //for schema builder
    
    public Task<Result<Schema[]>> All(SchemaType? type) => client.GetResult<Schema[]>($"/?type={type?.Camelize()}".ToSchemaApi());

    public Task<Result> Save(Schema schema) => client.PostResult("/".ToSchemaApi(), schema);

    public Task<Result<JsonElement>> Single(long id) => client.GetResult<JsonElement>($"/{id}".ToSchemaApi());


    public Task<Result> Delete(long id) => client.DeleteResult($"/{id}".ToSchemaApi());
    
    public Task<Result<Schema>> SaveEntityDefine(Schema schema)
        =>  client.PostResult<Schema>("/entity/define".ToSchemaApi(), schema);

    public Task<Result<Entity>> GetTableDefine(string table)
        =>  client.GetResult<Entity>($"/entity/{table}/define".ToSchemaApi());


    public async Task<bool> ExistsEntity(string entityName)
    {
        var res = await client.GetAsync($"/entity/{entityName}".ToSchemaApi());
        return res.IsSuccessStatusCode;
    }

    public Task<Result<Schema>> EnsureSimpleEntity(
        string entityName,
        string field,
        bool needPublish,
        string lookup = "",
        string junction = "",
        string collection = "",
        string linkAttribute = ""
    )
        => EnsureEntity(
            EntityHelper.CreateSimpleEntity(entityName, field, needPublish).AddLookUp(lookup).AddCollection(collection,linkAttribute).AddJunction(junction)
        );

   

    public Task<Result<Schema>> EnsureEntity(string entityName, string labelAttribute, bool needPublish,params Attribute[] attributes)
    {
        var entity = new Entity(
            PrimaryKey: DefaultAttributeNames.Id.Camelize(),
            Attributes:[..attributes],
            Name: entityName,
            TableName: entityName,
            DisplayName: entityName,
            LabelAttributeName: labelAttribute,
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus: needPublish ?PublicationStatus.Draft:PublicationStatus.Published
        );
        return EnsureEntity(entity);
    }
    
    public Task<Result<Schema>> EnsureEntity(Entity entity)
    {
        var url = $"/entity/add_or_update".ToSchemaApi();
        return client.PostResult<Schema>(url, entity);
    }

    public Task<Result> GraphQlClientUrl() => client.GetResult("/graphql".ToSchemaApi());
}

