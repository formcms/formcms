using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.ResultExt;
using IdGen;

using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class SchemaApiTest
{
    private AppFactory Factory { get; }
    private const string Name = "name";

    private readonly Schema _testSchema;
    
    public SchemaApiTest(AppFactory factory)
    {
        Factory = factory;
        new AuthApiClient(factory.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();
        var post = "schema_api_test_post" + new IdGenerator(0).CreateId();
        _testSchema = new(
            Name: post,
            Type: SchemaType.Entity,
            Settings: new Settings
            {
                Entity = new Entity(
                    Name: post,
                    PrimaryKey: "id",
                    DisplayName: post,
                    TableName: post,
                    LabelAttributeName: Name,
                    Attributes:
                    [
                        new Attribute
                        (
                            Field: Name,
                            DataType: DataType.String
                        )
                    ]
                )
            }
        );
    }

  
    [Fact]
    public async Task GetAll_NUllType()
    {
        var items = await Factory.SchemaApi.All(null).Ok();
        var len = items.Length;
        await Factory.SchemaApi.SaveEntityDefine(_testSchema);
        items = await Factory.SchemaApi.All(null).Ok();
        Assert.Equal(len + 1, items.Length);
    }

    [Fact]
    public async Task GetAll_EntityType()
    {
        var items = (await Factory.SchemaApi.All(SchemaType.Menu)).Ok();
        Assert.Single(items);
    }

    [Fact]
    public async Task GetTopMenuBar() => Assert.NotNull((await Factory.SchemaApi.GetTopMenuBar()).Ok().Name);

    [Fact]
    public async Task SaveSchemaAndOneAndGetLoaded()
    {
        var schema = (await Factory.SchemaApi.SaveEntityDefine(_testSchema)).Ok();
        await Factory.SchemaApi.GetLoadedEntity(schema.Name).Ok();
        await Factory.SchemaApi.Single(schema.Id).Ok();
    }

    [Fact]
    public async Task SaveSchemaTwice()
    {
        var res = (await Factory.SchemaApi.SaveEntityDefine(_testSchema)).Ok();
        (await Factory.SchemaApi.SaveEntityDefine(res)).Ok();
    }

    [Fact]
    public async Task SaveSchema_Update()
    {
        var schema = await Factory.SchemaApi.SaveEntityDefine(_testSchema).Ok();
        schema = schema with { Settings = new Settings(Entity: schema.Settings.Entity! with { DefaultPageSize = 10 }) };
        await Factory.SchemaApi.SaveEntityDefine(schema).Ok();
        var entity = (await Factory.SchemaApi.GetLoadedEntity(schema.Name)).Ok();
        Assert.Equal(10, entity.DefaultPageSize);
    }

    [Fact]
    public async Task Delete_Success()
    {
        var schema = (await Factory.SchemaApi.SaveEntityDefine(_testSchema)).Ok();
        await Factory.SchemaApi.Delete(schema.Id).Ok();
        Assert.True((await Factory.SchemaApi.GetLoadedEntity(schema.Name)).IsFailed);
    }

    [Fact]
    public async Task GetTableDefinitions_Success()
    {
        var schema = await Factory.SchemaApi.SaveEntityDefine(_testSchema).Ok();
        await Factory.SchemaApi.GetTableDefine(schema.Name).Ok();
    }

    [Fact]
    public async Task GetGraphQlClientUrlOk()
    {
        await Factory.SchemaApi.GraphQlClientUrl().Ok();
    }
}