using FormCMS.Utils.ResultExt;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.CoreKit.Test;

public enum TestFieldNames
{
    Name,
    Description,
    Image,
    Gallery,
    Post,
    
    Title,
    Abstract,
    Body,
    Attachments,
    Tags,
    Category,
    Authors,
}

public enum TestEntityNames
{
    TestPost,
    TestAuthor,
    TestTag,
    TestCategory,
    TestAttachment
}

public enum TestTableNames
{
    TestPosts,
    TestAuthors,
    TestTags,
    TestCategories,
    TestAttachments
}

public record EntityData(TestEntityNames EntityName, TestTableNames TableName, Record[] Records);
public record JunctionData(string EntityName,  string Attribute,string JunctionTableName, string SourceField, string TargetField, int SourceId, int[] TargetIds);

/// a set of blog entities to test queries
public static class BlogsTestData
{
    private static Attribute CreateAttr(this TestFieldNames fieldName) => new (fieldName.Camelize(), fieldName.Camelize());
    public static async Task EnsureBlogEntities(SchemaApiClient client)
    {
        await EnsureBlogEntities(x => client.EnsureEntity(x).Ok());
    }
    public static async Task EnsureBlogEntities(Func<Entity, Task> action)
    {
        foreach (var entity in Entities)
        {
            await  action(entity);
        }
    }

    public static async Task PopulateData(EntityApiClient client, AssetApiClient assetClient)
    {
        var list = new List<(string,byte[])>();
        for (var i = 0; i < 100; i++)
        {
            list.Add(($"{i}.txt", [1]));
        }
        var paths = await assetClient.AddAsset(list.ToArray()).Ok();
        
        await PopulateData(1, 100,paths.Split(","), async data =>
        {
            foreach (var dataRecord in data.Records)
            {
                await client.Insert(data.EntityName.Camelize(), dataRecord).Ok();
            }
        }, async data =>
        {
            //var items = data.TargetIds.Select(x => new { id = x });
            foreach (var dataTargetId in data.TargetIds)
            {
                await client.JunctionAdd(data.EntityName, data.Attribute, data.SourceId, dataTargetId).Ok();
            }
        });
    }

    public static async Task PopulateData(int startId, int count, string[]assetPaths, 
        Func<EntityData,Task> insertEntity, 
        Func<JunctionData,Task> insertJunction
        )
    {
        var tags = new List<Record>();
        var authors = new List<Record>();
        var categories = new List<Record>();
        var posts = new List<Record>();
        var tagsIds = new List<int>();
        var authorsIds = new List<int>();
        var attachments = new List<Record>();
        
        for (var i = startId; i < startId + count; i++)
        {
            tagsIds.Add(i);
            authorsIds.Add(i);
            
            tags.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description], i));
            authors.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description], i));
            categories.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description], i));
            
            var post = GetObject([TestFieldNames.Title, TestFieldNames.Abstract, TestFieldNames.Body], i);
            
            post[TestFieldNames.Category.Camelize()] = i;
            
            post[TestFieldNames.Image.Camelize()] = RandomGetString(1);
            post[TestFieldNames.Gallery.Camelize()] = RandomGetString(4);
            
            posts.Add(post);
            
            var attachment = GetObject([TestFieldNames.Name, TestFieldNames.Description], i);
            attachment["post"] = startId;
            attachments.Add(attachment);
        }

        await insertEntity(new EntityData(TestEntityNames.TestTag, TestTableNames.TestTags, tags.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestAuthor, TestTableNames.TestAuthors, authors.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestCategory, TestTableNames.TestCategories, categories.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestPost, TestTableNames.TestPosts, posts.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestAttachment, TestTableNames.TestAttachments, attachments.ToArray()));

        string RandomGetString(int i)
        {
            if (assetPaths.Length == 0)
                return string.Empty;
            i = Math.Min(i, assetPaths.Length);
            Random rand = new Random();
            var randomElements = assetPaths
                .OrderBy(x => rand.Next())  // Randomize order
                .Take(i)                   // Take first i elements
                .ToArray();
            return string.Join(",", randomElements);
        }
        
        await insertJunction(new JunctionData(
                TestEntityNames.TestPost.Camelize(),
                TestFieldNames.Tags.Camelize(),
                $"{TestEntityNames.TestPost.Camelize()}_{TestEntityNames.TestTag.Camelize()}",
                $"{TestEntityNames.TestPost.Camelize()}Id",
                $"{TestEntityNames.TestTag.Camelize()}Id",
                startId,
                tagsIds.ToArray()
            )
        );
        await insertJunction(new JunctionData(
                TestEntityNames.TestPost.Camelize(),
                TestFieldNames.Authors.Camelize(),
                $"{TestEntityNames.TestAuthor.Camelize()}_{TestEntityNames.TestPost.Camelize()}",
                $"{TestEntityNames.TestPost.Camelize()}Id",
                $"{TestEntityNames.TestAuthor.Camelize()}Id",
                startId,
                authorsIds.ToArray()
            )
        );
    }
    
    private static Dictionary<string,object> GetObject(TestFieldNames[] fields, int i)
    {
        var returnValue = new Dictionary<string, object>();
        foreach (var field in fields)
        {
            returnValue.Add(field.Camelize(), $"{field}-{i}");
        }
        returnValue[DefaultAttributeNames.PublicationStatus.Camelize()] =PublicationStatus.Published.Camelize();
        returnValue[DefaultAttributeNames.PublishedAt.Camelize()] = DateTime.UtcNow;
        return returnValue; 

    }

    
    private static readonly Entity[] Entities =
    [
        new(
            Name: TestEntityNames.TestTag.Camelize(),
            TableName: TestTableNames.TestTags.Camelize(),
            DisplayName: TestEntityNames.TestTag.ToString(),
            PrimaryKey:DefaultAttributeNames.Id.Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
            ],
            
            LabelAttributeName: TestFieldNames.Name.Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new(
            Name: TestEntityNames.TestAttachment.Camelize(),
            TableName: TestTableNames.TestAttachments.Camelize(),
            DisplayName: TestEntityNames.TestAttachment.ToString(),
            PrimaryKey:DefaultAttributeNames.Id.Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
                TestFieldNames.Post.CreateAttr() with{DataType = DataType.Int, DisplayType = DisplayType.Number}
                
            ],
            
            LabelAttributeName: TestFieldNames.Name.Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new(
            Name: TestEntityNames.TestAuthor.Camelize(),
            PrimaryKey:DefaultAttributeNames.Id.Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
            ],
            TableName: TestTableNames.TestAuthors.Camelize(),
            DisplayName: TestEntityNames.TestAuthor.ToString(),
            LabelAttributeName: TestFieldNames.Name.Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new (
            Name: TestEntityNames.TestCategory.Camelize(),
            PrimaryKey:DefaultAttributeNames.Id.Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
            ],
            TableName: TestTableNames.TestCategories.Camelize(),
            DisplayName: TestEntityNames.TestCategory.ToString(),
            
            LabelAttributeName: TestFieldNames.Name.Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new (
            Name: TestEntityNames.TestPost.Camelize(),
            PrimaryKey:DefaultAttributeNames.Id.Camelize(),
            Attributes:
            [
                TestFieldNames.Title.CreateAttr(),
                TestFieldNames.Abstract.CreateAttr(),
                TestFieldNames.Body.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},
                TestFieldNames.Gallery.CreateAttr() with{DisplayType = DisplayType.Gallery, DataType = DataType.Text},

                TestFieldNames.Attachments.CreateAttr() with{DisplayType = DisplayType.EditTable, DataType = DataType.Collection,
                    Options = $"{TestEntityNames.TestAttachment.Camelize()}.{TestFieldNames.Post.Camelize()}"},
                TestFieldNames.Tags.CreateAttr() with{DisplayType = DisplayType.Picklist, DataType = DataType.Junction, Options = TestEntityNames.TestTag.Camelize() },
                TestFieldNames.Authors.CreateAttr() with{DisplayType = DisplayType.Picklist, DataType = DataType.Junction, Options = TestEntityNames.TestAuthor.Camelize() },
                TestFieldNames.Category.CreateAttr() with{DataType = DataType.Lookup, DisplayType = DisplayType.Lookup, Options = TestEntityNames.TestCategory.Camelize() },
            ],
            DisplayName: TestEntityNames.TestPost.ToString(),
            TableName: TestTableNames.TestPosts.Camelize(),
            
            LabelAttributeName: TestFieldNames.Title.Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        )
    ];

}