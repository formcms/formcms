using System.Text.Json;
using FormCMS.Auth.ApiClient;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.ResultExt;
using FormCMS.Infrastructure.RelationDbDao;

using Microsoft.Extensions.Primitives;
using NUlid;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class EntityApiTest(AppFactory factory)
{
    private const string Name = "name";
    private readonly string _post = "post_" + Ulid.NewUlid();
    private readonly string _author = "author_" + Ulid.NewUlid();
    private readonly string _tag = "tag_" + Ulid.NewUlid();
    private readonly string _attachment = "att_" + Ulid.NewUlid();
    private readonly string _category = "cat_" + Ulid.NewUlid();

    private static readonly string[] Payload = ["a", "b", "c"];
    private bool _ = factory.LoginAndInitTestData();
 

    [Fact]
    public async Task InsertAndQueryDateField()
    {
        await factory.SchemaApi.EnsureEntity(_post, Name, false,
            new Attribute(Name, Name),
            new Attribute("start", "start", DataType: DataType.Datetime, DisplayType: DisplayType.Date)
        ).Ok();
        await factory.EntityApi.Insert(_post, new { name = "post1", start = "2025-01-01" }).Ok();
        await factory.EntityApi.Insert(_post, new { name = "post2", start = "2025-01-02" }).Ok();
        await factory.EntityApi.Insert(_post, new { name = "post3", start = "2025-01-03" }).Ok();

        var res = await factory.EntityApi.List(_post, new Dictionary<string, StringValues>
        {
            { "offset", "0" },
            { "limit", "100" },
            { "start[dateAfter]", "2025-01-01" }
        }).Ok();
        Assert.Equal(2, res.TotalRecords);
    }

    [Fact]
    public async Task PublishUnpublishEntity()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, true).Ok();
        await factory.EntityApi.Insert(_post, Name, "name1").Ok();
        var ele = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.Equal(PublicationStatus.Draft.Camelize(),
            ele.GetProperty(DefaultAttributeNames.PublicationStatus.Camelize()).GetString());

        //publish
        var payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.Camelize(), PublicationStatus.Published.Camelize() },
            { DefaultAttributeNames.PublishedAt.Camelize(), new DateTime(2025, 1, 1) }
        };
        await factory.EntityApi.SavePublicationSettings(_post, payload).Ok();
        ele = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.Equal(PublicationStatus.Published.Camelize(),
            ele.GetProperty(DefaultAttributeNames.PublicationStatus.Camelize()).GetString());

        //unpublish
        payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.Camelize(), PublicationStatus.Unpublished.Camelize() },
        };
        await factory.EntityApi.SavePublicationSettings(_post, payload).Ok();
        ele = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.Equal(
            PublicationStatus.Unpublished.Camelize(),
            ele.GetProperty(DefaultAttributeNames.PublicationStatus.Camelize()).GetString());

        //scheduled
        payload = new Dictionary<string, object>
        {
            { DefaultAttributeNames.Id.Camelize(), 1 },
            { DefaultAttributeNames.PublicationStatus.Camelize(), PublicationStatus.Scheduled.Camelize() },
            { DefaultAttributeNames.PublishedAt.Camelize(), new DateTime(2025, 1, 1) }
        };

        await factory.EntityApi.SavePublicationSettings(_post, payload).Ok();
        ele = await factory.EntityApi.Single(_post, 1).Ok();

        Assert.Equal(PublicationStatus.Scheduled.Camelize(),
            ele.GetProperty(DefaultAttributeNames.PublicationStatus.Camelize()).GetString());
        Assert.True(
            ele.TryGetProperty(DefaultAttributeNames.PublishedAt.Camelize(), out var publishEle)
            && DateTime.TryParse(publishEle.GetString(), out var publishedAt)
            && publishedAt.Equals(new DateTime(2025, 1, 1))
        );
    }

    [Fact]
    public async Task DropdownAttributeMustHaveOptions()
    {
        var res = await factory.SchemaApi.EnsureEntity(
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
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        var res = await factory.EntityApi.Insert(_post, Name, null!);
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task ValidationRule()
    {
        var attr = new Attribute(Name, Name, Validation: $"""
                                                          {Name}==null?"name-null-fail":""
                                                          """);
        await factory.SchemaApi.EnsureEntity(_post, Name, false, attr).Ok();
        var res = await factory.EntityApi.Insert(_post, Name, null!);
        Assert.True(res.IsFailed && res.Errors[0].Message.Contains("name-null-fail"));
    }

    [Fact]
    public async Task VerifyRegexValidator()
    {
        var attr = new Attribute(Name, Name, Validation: $"""
                                                          Regex.IsMatch({Name}, "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$")?"":"regex-match-fail"
                                                          """);
        await factory.SchemaApi.EnsureEntity(_post, Name, false, attr).Ok();
        var res = await factory.EntityApi.Insert(_post, Name, "aa");
        Assert.True(res.IsFailed && res.Errors[0].Message.Contains("regex-match-fail"));
    }

    [Fact]
    public async Task TestMultiSelect()
    {
        var attr = new Attribute(Name, Name, DisplayType: DisplayType.Multiselect, Options: "a,b,c,d,e,f");
        await factory.SchemaApi.EnsureEntity(_post, Name, false, attr).Ok();

        await factory.EntityApi.Insert(_post, new { name = Payload }).Ok();
        var ele = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.True(ele.TryGetProperty(Name, out var val) && val.ValueKind == JsonValueKind.Array &&
                    val.GetArrayLength() == 3);
    }

    [Fact]
    public async Task TestGallery()
    {
        var attr = new Attribute(Name, Name, DisplayType: DisplayType.Gallery);
        await factory.SchemaApi.EnsureEntity(_post, Name, false, attr).Ok();

        await factory.EntityApi.Insert(_post, new { name = new[] { "a", "b", "c" } }).Ok();
        var ele = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.True(ele.TryGetProperty(Name, out var val) && val.ValueKind == JsonValueKind.Array &&
                    val.GetArrayLength() == 3);
    }

    [Fact]
    public async Task TestDictionary()
    {
        var attr = new Attribute(Name, Name, DataType:DataType.Text, DisplayType: DisplayType.Dictionary);
        await factory.SchemaApi.EnsureEntity(_post, Name, false, attr).Ok();
        var dict = new
        {
            a = 1,
            b = 2,
        };

        await factory.EntityApi.Insert(_post, new { name = dict }).Ok();
        var ele = await factory.EntityApi.Single(_post, 1).Ok();
        
        Assert.True(
            ele.TryGetProperty(Name, out var val) 
            && val.ValueKind == JsonValueKind.Object  
            && val.GetProperty("a").GetInt64() == 1
            && val.GetProperty("b").GetInt64() == 2
            );
        
    }

    [Fact]
    public async Task TestResponseMode()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        await factory.EntityApi.Insert(_post, Name, "post1").Ok();
        var response = await factory.EntityApi.List(_post, 0, 1, "count").Ok();
        Assert.True(response.Items.Length == 0);

        response = await factory.EntityApi.List(_post, 0, 1, "items").Ok();
        Assert.True(response.TotalRecords == 0);
    }

    [Fact]
    public async Task GetResultAsTree()
    {
        Attribute[] attrs =
        [
            new(Name, Name),
            new("parent", "Parent", DataType: DataType.Int, DisplayType: DisplayType.Number),
            new("children", "Children", DataType: DataType.Collection, DisplayType: DisplayType.EditTable,
                Options: $"{_category}.parent"),
        ];

        await factory.SchemaApi.EnsureEntity(_category, Name, false, attrs).Ok();
        await factory.EntityApi.Insert(_category, new { name = "cat1", }).Ok();
        await factory.EntityApi.Insert(_category, new { name = "cat2", }).Ok();
        await factory.EntityApi.Insert(_category, new { name = "cat3", }).Ok();
        await factory.EntityApi.CollectionInsert(_category, "children", 1, new { name = "cat1-1" }).Ok();
        await factory.EntityApi.CollectionInsert(_category, "children", 1, new { name = "cat1-2" }).Ok();
        var items = await factory.EntityApi.ListAsTree(_category).Ok();
        var children = items[0].GetProperty("children");
        Assert.Equal(JsonValueKind.Array, children.ValueKind);
        Assert.Equal(2, children.GetArrayLength());
    }

    [Fact]
    public async Task PreventDirtyWrite()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        await factory.EntityApi.Insert(_post, Name, "post1").Ok();

        //make sure get the latest data
        var item = await factory.EntityApi.Single(_post, 1).Ok();
        var updatedAt = item.GetProperty(DefaultColumnNames.UpdatedAt.Camelize()).GetString()!;

        Thread.Sleep(TimeSpan.FromSeconds(1));
        await factory.EntityApi.Update(_post, 1, Name, "post2", updatedAt).Ok();
        var newItem = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.True(newItem.GetProperty(DefaultColumnNames.UpdatedAt.Camelize()).GetString() != updatedAt);

        //now updatedAt has changed, any dirty writing should fail
        var res = await factory.EntityApi.Delete(_post, item);
        Assert.True(res.IsFailed);

        res = await factory.EntityApi.Update(_post, 1, Name, "post3", updatedAt);
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertListDeleteOk()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        await factory.EntityApi.Insert(_post, Name, "post1").Ok();

        var res = await factory.EntityApi.List(_post, 0, 10).Ok();
        Assert.Single(res.Items);

        //make sure get the latest data
        var item = await factory.EntityApi.Single(_post, 1).Ok();

        await factory.EntityApi.Delete(_post, item).Ok();
        res = await factory.EntityApi.List(_post, 0, 10).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task InsertAndUpdateAndOneOk()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        var item = await factory.EntityApi.Insert(_post, Name, "post1").Ok();
        Assert.Equal(1L, item.GetProperty("id").GetInt64());

        item = await factory.EntityApi.Single(_post, 1).Ok();
        var updatedAt = item.GetProperty(DefaultColumnNames.UpdatedAt.Camelize()).GetString()!;

        await factory.EntityApi.Update(_post, 1, Name, "post2", updatedAt).Ok();

        item = await factory.EntityApi.Single(_post, 1).Ok();
        Assert.Equal("post2", item.GetProperty(Name).GetString());
    }

    [Fact]
    public async Task ListWithPaginationOk()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        for (var i = 0; i < 5; i++)
        {
            (await factory.EntityApi.Insert(_post, Name, $"student{i}")).Ok();
        }

        await factory.EntityApi.Insert(_post, Name, "good-student").Ok();
        await factory.EntityApi.Insert(_post, Name, "good-student").Ok();

        //get first page
        Assert.Equal(5, (await factory.EntityApi.List(_post, 0, 5)).Ok().Items.Length);
        //get last page
        var res = (await factory.EntityApi.List(_post, 5, 5)).Ok();
        Assert.Equal(2, res.Items.Length);
    }

    [Fact]
    public async Task InsertLookupWithWrongData()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_author, Name, false).Ok();
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false, lookup: _author).Ok();
        var res = await factory.EntityApi.InsertWithLookup(_post, Name, "post1",
            _author, "author1");
        Assert.True(res.IsFailed);
    }

    [Fact]
    public async Task InsertWithLookup()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_author, Name, false).Ok();
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false, lookup: _author).Ok();
        var author = (await factory.EntityApi.Insert(_author, Name, "author1")).Ok();
        await factory.EntityApi.InsertWithLookup(_post, Name, "post1",
            _author, author.GetProperty("id").GetInt64()).Ok();
    }

    [Fact]
    public async Task InsertDeleteListJunction()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_tag, Name, false).Ok();
        await factory.EntityApi.Insert(_tag, Name, "tag1").Ok();

        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false, junction: _tag).Ok();
        await factory.EntityApi.Insert(_post, Name, "post1").Ok();


        await factory.EntityApi.JunctionAdd(_post, _tag, 1, 1).Ok();
        var res = await factory.EntityApi.JunctionList(_post, _tag, 1, true).Ok();
        Assert.Empty(res.Items);

        var ids = await factory.EntityApi.JunctionTargetIds(_post, _tag, 1).Ok();
        Assert.Single(ids);

        res = await factory.EntityApi.JunctionList(_post, _tag, 1, false).Ok();
        Assert.Single(res.Items);

        await factory.EntityApi.JunctionDelete(_post, _tag, 1, 1).Ok();
        res = await factory.EntityApi.JunctionList(_post, _tag, 1, true).Ok();
        Assert.Single(res.Items);
        res = await factory.EntityApi.JunctionList(_post, _tag, 1, false).Ok();
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task LookupApiWorks()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_tag, Name, false).Ok();
        for (var i = 0; i < EntityConstants.DefaultPageSize - 1; i++)
        {
            await factory.EntityApi.Insert(_tag, Name, $"tag{i}");
        }

        var res = await factory.EntityApi.LookupList(_tag, "").Ok();
        Assert.False(res.GetProperty("hasMore").GetBoolean());

        for (var i = EntityConstants.DefaultPageSize; i < EntityConstants.DefaultPageSize + 10; i++)
        {
            await factory.EntityApi.Insert(_tag, Name, $"tag{i}");
        }

        res = await factory.EntityApi.LookupList(_tag, "").Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());

        res = await factory.EntityApi.LookupList(_tag, "tag11").Ok();
        Assert.True(res.GetProperty("hasMore").GetBoolean());
        Assert.Equal(1, res.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task CollectionApiWorks()
    {
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false).Ok();
        await factory.SchemaApi.EnsureSimpleEntity(_attachment, Name, false, lookup: _post).Ok();
        await factory.SchemaApi.EnsureSimpleEntity(_post, Name, false, collection: _attachment, linkAttribute: _post)
            .Ok();

        await factory.EntityApi.Insert(_post, Name, "post1").Ok();

        await factory.EntityApi.CollectionInsert(_post, _attachment, 1,
            new Dictionary<string, object> { { Name, "attachment1" } }).Ok();

        var listResponse = await factory.EntityApi.CollectionList(_post, _attachment, 1).Ok();
        Assert.Single(listResponse.Items);

        var item = await factory.EntityApi.Single(_attachment, 1).Ok();
        await factory.EntityApi.Delete(_attachment, item).Ok();
        listResponse = await factory.EntityApi.CollectionList(_post, _attachment, 1).Ok();
        Assert.Empty(listResponse.Items);

    }
}