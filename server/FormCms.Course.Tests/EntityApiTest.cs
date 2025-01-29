using System.Text.Json;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using IdGen;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;

public class EntityApiTest
{
    private const string Name = "name";
    private readonly  string _post = "entity_api_test_post" + new IdGenerator(0).CreateId();
    private readonly string _author = "entity_api_test_author" + new IdGenerator(0).CreateId();
    private readonly string _tag = "entity_api_test_tag" + new IdGenerator(0).CreateId();
    private readonly string _attachment = "entity_api_test_attachment" + new IdGenerator(0).CreateId();
    private readonly string _category = "entity_api_test_category" + new IdGenerator(0).CreateId();

    private readonly EntityApiClient _entityApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private static readonly string[] Payload = ["a","b","c"];

    public EntityApiTest()
    {
        // Util.SetTestConnectionString();

        WebAppClient<Program> webAppClient = new();
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task PublishUnpublishEntity()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name, true).Ok();
        await _entityApiClient.Insert(_post, Name, "name1").Ok();
        var ele = await _entityApiClient.Single(_post, 1).Ok();
        Assert.Equal(PublicationStatus.Draft.ToString().Camelize(), ele.GetProperty(DefaultAttributeNames.PublicationStatus.ToString().Camelize()).GetString());

        //publish
        var payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.ToString().Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.ToString().Camelize(), PublicationStatus.Published.ToString().Camelize() },
            { DefaultAttributeNames.PublishedAt.ToString().Camelize(), new DateTime(2025, 1, 1) }
        };
        await _entityApiClient.SavePublicationSettings(_post,payload).Ok();
        ele = await _entityApiClient.Single(_post, 1).Ok();
        Assert.Equal(PublicationStatus.Published.ToString().Camelize(), ele.GetProperty(DefaultAttributeNames.PublicationStatus.ToString().Camelize()).GetString());

        //unpublish
        payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.ToString().Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.ToString().Camelize(), PublicationStatus.Unpublished.ToString().Camelize() },
        };
        await _entityApiClient.SavePublicationSettings(_post,payload).Ok();
        ele = await _entityApiClient.Single(_post, 1).Ok();
        Assert.Equal(
            PublicationStatus.Unpublished.ToString().Camelize(), 
            ele.GetProperty(DefaultAttributeNames.PublicationStatus.ToString().Camelize()).GetString());

        //scheduled
        payload = new Dictionary<string,object>
        {
            { DefaultAttributeNames.Id.ToString().Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.ToString().Camelize(), PublicationStatus.Scheduled.ToString().Camelize()},
            { DefaultAttributeNames.PublishedAt.ToString().Camelize(), new DateTime(2025,1,1)}
        };
        
        await _entityApiClient.SavePublicationSettings(_post,payload).Ok();
        ele = await _entityApiClient.Single(_post, 1).Ok();
        
        Assert.Equal(PublicationStatus.Scheduled.ToString().Camelize(), ele.GetProperty(DefaultAttributeNames.PublicationStatus.ToString().Camelize()).GetString());
        Assert.True(
            ele.TryGetProperty(DefaultAttributeNames.PublishedAt.ToString().Camelize(), out var publishEle) 
            && DateTime.TryParse(publishEle.GetString(), out var publishedAt)
            && publishedAt.Equals(new DateTime(2025,1,1))
        );
    }
    
    [Fact]
    public async Task DropdownAttributeMustHaveOptions()
    {
        var res = await _schemaApiClient.EnsureEntity(
            _post,
            "name",
            false,
            new Attribute("name", "Name", DisplayType: DisplayType.Dropdown)
        );
        Assert.True(res.IsFailed);
    }
    
    [Fact]
    public async Task CannotInsertNullTitleEntity()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name, false).Ok();
        var res = await _entityApiClient.Insert(_post, Name, null!);
        Assert.True(res.IsFailed);
    }
    [Fact]
    public async Task ValidationRule()
    {
        var attr = new Attribute(Name, Name, Validation:$"""
                                                            {Name}==null?"name-null-fail":""
                                                            """);
        await _schemaApiClient.EnsureEntity(_post, Name,false,attr).Ok();
        var res = await _entityApiClient.Insert(_post, Name, null!);
        Assert.True(res.IsFailed && res.Errors[0].Message.Contains("name-null-fail"));
    }

    [Fact]
    public async Task VerifyRegexValidator()
    {
        var attr = new Attribute(Name, Name, Validation:$"""
                                                            Regex.IsMatch({Name}, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")?"":"regex-match-fail"
                                                            """);
        await _schemaApiClient.EnsureEntity(_post, Name,false,attr).Ok();
        var res = await _entityApiClient.Insert(_post, Name, "aa");
        Assert.True(res.IsFailed && res.Errors[0].Message.Contains("regex-match-fail"));
    }

    [Fact]
    public async Task TestMultiSelect()
    {
        var attr = new Attribute(Name, Name, DisplayType: DisplayType.Multiselect, Options: "a,b,c,d,e,f");
        await _schemaApiClient.EnsureEntity(_post,Name,false,attr).Ok();

        await _entityApiClient.Insert(_post, new { name = Payload }).Ok();
        var ele = await _entityApiClient.Single(_post, 1).Ok();
        Assert.True(ele.TryGetProperty(Name,out var val ) && val.ValueKind == JsonValueKind.Array && val.GetArrayLength() == 3);
    }
     [Fact]
     public async Task TestGallery()
     {
         var attr = new Attribute(Name, Name, DisplayType: DisplayType.Gallery);
         await _schemaApiClient.EnsureEntity(_post,Name,false,attr).Ok();
 
         await _entityApiClient.Insert(_post, new { name = new []{"a","b","c"}}).Ok();
         var ele = await _entityApiClient.Single(_post, 1).Ok();
         Assert.True(ele.TryGetProperty(Name,out var val ) && val.ValueKind == JsonValueKind.Array && val.GetArrayLength() == 3);
     }
     
    [Fact]
    public async Task TestResponseMode()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
        await _entityApiClient.Insert(_post, Name, "post1").Ok();
        var response = await _entityApiClient.List(_post, 0, 1, "count").Ok();
        Assert.True(response.Items.Length == 0);

        response = await _entityApiClient.List(_post, 0, 1, "items").Ok();
        Assert.True(response.TotalRecords == 0);
    }
    
    [Fact]
    public async Task GetResultAsTree()
    {
        Attribute[] attrs =
        [
            new (Name, Name),
            new ("parent", "Parent", DataType: DataType.Int, DisplayType: DisplayType.Number),
            new ("children", "Children", DataType: DataType.Collection, DisplayType: DisplayType.EditTable, Options: $"{_category}.parent"),
        ];

        await _schemaApiClient.EnsureEntity(_category,Name,false,attrs).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat1", }).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat2", }).Ok();
        await _entityApiClient.Insert(_category, new { name = "cat3", }).Ok();
        await _entityApiClient.CollectionInsert(_category, "children", 1, new { name= "cat1-1" }).Ok();
        await _entityApiClient.CollectionInsert(_category, "children", 1, new { name= "cat1-2" }).Ok();
        var items = await _entityApiClient.ListAsTree(_category).Ok();
        var children = items[0].GetProperty("children");
        Assert.Equal(JsonValueKind.Array,children.ValueKind);
        Assert.Equal(2, children.GetArrayLength());
    }

    [Fact]
    public async Task PreventDirtyWrite()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name, false).Ok();
        await _entityApiClient.Insert(_post, Name, "post1").Ok();

        //make sure get the latest data
        var item = await _entityApiClient.Single(_post, 1).Ok();
        var updatedAt = item.GetProperty(DefaultAttributeNames.UpdatedAt.ToString().Camelize()).GetString()!;

        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
        await _entityApiClient.Update(_post, 1, Name, "post2", updatedAt).Ok();
        var newItem = await _entityApiClient.Single(_post, 1).Ok();
        Assert.True(newItem.GetProperty(DefaultAttributeNames.UpdatedAt.ToString().Camelize()).GetString() != updatedAt);

        //now updatedAt has changed, any dirty writing should fail
        var res = await _entityApiClient.Delete(_post, item);
        Assert.True(res.IsFailed);

        res = await _entityApiClient.Update(_post, 1, Name, "post3", updatedAt);
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertListDeleteOk()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
        await _entityApiClient.Insert(_post, Name, "post1").Ok();

        var res = await _entityApiClient.List(_post, 0, 10).Ok();
        Assert.Single(res.Items);

        //make sure get the latest data
        var item = await _entityApiClient.Single(_post, 1).Ok();

        await _entityApiClient.Delete(_post, item).Ok();
        res = await _entityApiClient.List(_post, 0, 10).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task InsertAndUpdateAndOneOk()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
        var item = await _entityApiClient.Insert(_post, Name, "post1").Ok();
        Assert.True(item.ToDictionary().TryGetValue("id", out var element));
        Assert.Equal(1, element);
        
        item = await _entityApiClient.Single(_post, 1).Ok();
        var updatedAt = item.GetProperty(DefaultAttributeNames.UpdatedAt.ToString().Camelize()).GetString()!;

        await _entityApiClient.Update(_post, 1, Name, "post2", updatedAt).Ok();

        item = await _entityApiClient.Single(_post, 1).Ok();
        Assert.True(item.ToDictionary().TryGetValue(Name, out element));
        Assert.Equal("post2", element);
    }

    [Fact]
    public async Task ListWithPaginationOk()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
        for (var i = 0; i < 5; i++)
        {
            (await _entityApiClient.Insert(_post, Name, $"student{i}")).Ok();
        }

        await _entityApiClient.Insert(_post, Name, "good-student").Ok();
        await _entityApiClient.Insert(_post, Name, "good-student").Ok();

        //get first page
        Assert.Equal(5, (await _entityApiClient.List(_post, 0, 5)).Ok().Items.Length);
        //get last page
        var res = (await _entityApiClient.List(_post, 5, 5)).Ok();
        Assert.Equal(2, res.Items.Length);
    }

    [Fact]
    public async Task InsertWithLookupWithWrongData()
    {
        await _schemaApiClient.EnsureSimpleEntity(_author, Name,false).Ok();
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false,lookup: _author).Ok();
        var res = await _entityApiClient.InsertWithLookup(_post, Name, "post1",
            _author, "author1");
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertWithLookup()
    {
        await _schemaApiClient.EnsureSimpleEntity(_author, Name,false).Ok();
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false, lookup:_author).Ok();
        var author = (await _entityApiClient.Insert(_author, Name, "author1")).Ok();
        await _entityApiClient.InsertWithLookup(_post, Name, "post1",
            _author, author.ToDictionary()["id"]).Ok();
    }

    [Fact]
    public async Task InsertDeleteListJunction()
    {
        await _schemaApiClient.EnsureSimpleEntity(_tag, Name,false).Ok();
        await _entityApiClient.Insert(_tag, Name, "tag1").Ok();

        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false, junction: _tag).Ok();
        await _entityApiClient.Insert(_post, Name, "post1").Ok();
        

        await _entityApiClient.JunctionAdd(_post, _tag, 1, 1).Ok();
        var res = await _entityApiClient.JunctionList(_post, _tag, 1, true).Ok();
        Assert.Empty(res.Items);
        
        var ids = await _entityApiClient.JunctionTargetIds(_post, _tag, 1).Ok();
        Assert.Single(ids);
        
        res = await _entityApiClient.JunctionList(_post, _tag, 1, false).Ok();
        Assert.Single(res.Items);

        await _entityApiClient.JunctionDelete(_post, _tag, 1, 1).Ok();
        res = await _entityApiClient.JunctionList(_post, _tag, 1, true).Ok();
        Assert.Single(res.Items);
        res = await _entityApiClient.JunctionList(_post, _tag, 1, false).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task LookupApiWorks()
    {
        await _schemaApiClient.EnsureSimpleEntity(_tag, Name,false).Ok();
        for (var i = 0; i < EntityConstants.DefaultPageSize - 1; i++)
        {
            await _entityApiClient.Insert(_tag,Name,$"tag{i}");
        }

        var res = await _entityApiClient.LookupList(_tag,"").Ok();
        Assert.False(res.GetProperty("hasMore").GetBoolean());

        for (var i = EntityConstants.DefaultPageSize; i < EntityConstants.DefaultPageSize + 10; i++)
        {
            await _entityApiClient.Insert(_tag,Name,$"tag{i}");
        }
        
        res = await _entityApiClient.LookupList(_tag,"").Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());

        res = await _entityApiClient.LookupList(_tag,"tag11").Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());
        Assert.Equal(1,res.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task CollectionApiWorks()
    {
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false).Ok();
        await _schemaApiClient.EnsureSimpleEntity(_attachment, Name,false,lookup: _post).Ok();
        await _schemaApiClient.EnsureSimpleEntity(_post, Name,false,collection:_attachment,linkAttribute:_post).Ok();

        await _entityApiClient.Insert(_post, Name, "post1").Ok();

        await _entityApiClient.CollectionInsert(_post, _attachment, 1,
            new Dictionary<string, object> {{Name,"attachment1" }}).Ok();

        var listResponse = await _entityApiClient.CollectionList(_post, _attachment, 1).Ok();
        Assert.Single(listResponse.Items);

        var item = await _entityApiClient.Single(_attachment, 1).Ok();
        await _entityApiClient.Delete(_attachment, item).Ok();
        listResponse = await _entityApiClient.CollectionList(_post, _attachment, 1).Ok();
        Assert.Empty(listResponse.Items);

    }
}