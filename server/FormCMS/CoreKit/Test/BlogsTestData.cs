using FormCMS.Utils.ResultExt;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.CoreKit.Test;

public enum TestFieldNames
{
    Name,
    Description,
    Image,
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
    private static Attribute CreateAttr(this TestFieldNames fieldName) => new Attribute(fieldName.ToString().Camelize(), fieldName.ToString().Camelize());
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

    public static async Task PopulateData(EntityApiClient client)
    {
        await PopulateData(1, 100, async data =>
        {
            foreach (var dataRecord in data.Records)
            {
                await client.Insert(data.EntityName.ToString().Camelize(), dataRecord).Ok();
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

    public static async Task PopulateData(int startId, int count, Func<EntityData,Task> insertEntity, Func<JunctionData,Task> insertJunction)
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
            
            tags.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description, TestFieldNames.Image], i));
            authors.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description, TestFieldNames.Image], i));
            categories.Add(GetObject([TestFieldNames.Name, TestFieldNames.Description, TestFieldNames.Image], i));
            
            var post = GetObject([TestFieldNames.Title, TestFieldNames.Abstract, TestFieldNames.Body, TestFieldNames.Image], i);
            post[TestFieldNames.Category.ToString().Camelize()] = i;
            posts.Add(post);
            
            var attachment = GetObject([TestFieldNames.Name, TestFieldNames.Description, TestFieldNames.Image], i);
            attachment["post"] = startId;
            attachments.Add(attachment);
        }

        await insertEntity(new EntityData(TestEntityNames.TestTag, TestTableNames.TestTags, tags.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestAuthor, TestTableNames.TestAuthors, authors.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestCategory, TestTableNames.TestCategories, categories.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestPost, TestTableNames.TestPosts, posts.ToArray()));
        await insertEntity(new EntityData(TestEntityNames.TestAttachment, TestTableNames.TestAttachments, attachments.ToArray()));

        await insertJunction(new JunctionData(
                TestEntityNames.TestPost.ToString().Camelize(),
                TestFieldNames.Tags.ToString().Camelize(),
                $"{TestEntityNames.TestPost.ToString().Camelize()}_{TestEntityNames.TestTag.ToString().Camelize()}",
                $"{TestEntityNames.TestPost.ToString().Camelize()}Id",
                $"{TestEntityNames.TestTag.ToString().Camelize()}Id",
                startId,
                tagsIds.ToArray()
            )
        );
        await insertJunction(new JunctionData(
                TestEntityNames.TestPost.ToString().Camelize(),
                TestFieldNames.Authors.ToString().Camelize(),
                $"{TestEntityNames.TestAuthor.ToString().Camelize()}_{TestEntityNames.TestPost.ToString().Camelize()}",
                $"{TestEntityNames.TestPost.ToString().Camelize()}Id",
                $"{TestEntityNames.TestAuthor.ToString().Camelize()}Id",
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
            returnValue.Add(field.ToString().Camelize(), $"{field}-{i}");
        }
        returnValue.SetCamelKeyCamelValue(DefaultAttributeNames.PublicationStatus,PublicationStatus.Published);
        returnValue.SetCamelKey(DefaultAttributeNames.PublishedAt,DateTime.Now);
        return returnValue; 

    }

    
    private static readonly Entity[] Entities =
    [
        new(
            PrimaryKey:DefaultAttributeNames.Id.ToString().Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},
            ],
            TableName: TestTableNames.TestTags.ToString().Camelize(),
            DisplayName: TestEntityNames.TestTag.ToString(),
            Name: TestEntityNames.TestTag.ToString().Camelize(),
            
            LabelAttributeName: TestFieldNames.Name.ToString().Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new(
            PrimaryKey:DefaultAttributeNames.Id.ToString().Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},
                TestFieldNames.Post.CreateAttr() with{DataType = DataType.Int, DisplayType = DisplayType.Number}
                
            ],
            TableName: TestTableNames.TestAttachments.ToString().Camelize(),
            DisplayName: TestEntityNames.TestAttachment.ToString(),
            Name: TestEntityNames.TestAttachment.ToString().Camelize(),
            
            LabelAttributeName: TestFieldNames.Name.ToString().Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new(
            PrimaryKey:DefaultAttributeNames.Id.ToString().Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},
            ],
            TableName: TestTableNames.TestAuthors.ToString().Camelize(),
            DisplayName: TestEntityNames.TestAuthor.ToString(),
            Name: TestEntityNames.TestAuthor.ToString().Camelize(),
            LabelAttributeName: TestFieldNames.Name.ToString().Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new (
            PrimaryKey:DefaultAttributeNames.Id.ToString().Camelize(),
            Attributes:
            [
                TestFieldNames.Name.CreateAttr(),
                TestFieldNames.Description.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},
            ],
            TableName: TestTableNames.TestCategories.ToString().Camelize(),
            DisplayName: TestEntityNames.TestCategory.ToString(),
            Name: TestEntityNames.TestCategory.ToString().Camelize(),
            
            LabelAttributeName: TestFieldNames.Name.ToString().Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        ),
        new (
            PrimaryKey:DefaultAttributeNames.Id.ToString().Camelize(),
            Attributes:
            [
                TestFieldNames.Title.CreateAttr(),
                TestFieldNames.Abstract.CreateAttr(),
                TestFieldNames.Body.CreateAttr(),
                TestFieldNames.Image.CreateAttr() with{DisplayType = DisplayType.Image},

                TestFieldNames.Attachments.CreateAttr() with{DisplayType = DisplayType.EditTable, DataType = DataType.Collection,
                    Options = $"{TestEntityNames.TestAttachment.ToString().Camelize()}.{TestFieldNames.Post.ToString().Camelize()}"},
                TestFieldNames.Tags.CreateAttr() with{DisplayType = DisplayType.Picklist, DataType = DataType.Junction, Options = TestEntityNames.TestTag.ToString().Camelize() },
                TestFieldNames.Authors.CreateAttr() with{DisplayType = DisplayType.Picklist, DataType = DataType.Junction, Options = TestEntityNames.TestAuthor.ToString().Camelize() },
                TestFieldNames.Category.CreateAttr() with{DataType = DataType.Lookup, DisplayType = DisplayType.Lookup, Options = TestEntityNames.TestCategory.ToString().Camelize() },
            ],
            Name: TestEntityNames.TestPost.ToString().Camelize(),
            DisplayName: TestEntityNames.TestPost.ToString(),
            TableName: TestTableNames.TestPosts.ToString().Camelize(),
            
            LabelAttributeName: TestFieldNames.Title.ToString().Camelize(),
            DefaultPageSize: EntityConstants.DefaultPageSize,
            DefaultPublicationStatus:PublicationStatus.Published
        )
    ];

}