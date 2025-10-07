using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Title.Models;
using OrchardCore.Autoroute.Models;
using OrchardCore.Html.Models;
using OrchardCore.Environment.Shell.Scope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySite;
public class PostPart : ContentPart
{
    public ContentPickerField Tags { get; set; } = new();
    public ContentPickerField Categories { get; set; } = new();
}
[Route("seed")]
[ApiController]
public class SeedController(ILogger<SeedController> logger,IContentManager contentManager,IContentDefinitionManager definitionManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Seed([FromQuery] int start = 0)
    {
        if (start == 0)
        {
            await CreateContentTypes();
        }

        var tags = await SeedItems("Tag", start,100);
        var categories = await SeedItems("Category", start,10);
        await SeedPosts(tags, categories, start,1000);
        return Ok("done");
    }

    private async Task CreateContentTypes()
    {
        logger.LogCritical("Creating content types...");

        // Tag
        await definitionManager.AlterTypeDefinitionAsync("Tag", type => type
            .DisplayedAs("Tag")
            .Creatable()
            .Listable()
            .Draftable()
            .WithPart("TitlePart", part => part
                .WithSettings(new TitlePartSettings
                {
                    RenderTitle = true
                })));

        // Category
        await definitionManager.AlterTypeDefinitionAsync("Category", type => type
            .DisplayedAs("Category")
            .Creatable()
            .Listable()
            .Draftable()
            .WithPart("TitlePart", part => part
                .WithSettings(new TitlePartSettings
                {
                    RenderTitle = true
                })));

        // PostPart with pickers
        await definitionManager.AlterPartDefinitionAsync("PostPart", part => part
            .WithField("Tags", field => field
                .OfType("ContentPickerField")
                .WithDisplayName("Tags")
                .WithSettings(new ContentPickerFieldSettings
                {
                    DisplayedContentTypes = new[] { "Tag" },
                    Multiple = true,
                    Required = false
                }))
            .WithField("Categories", field => field
                .OfType("ContentPickerField")
                .WithDisplayName("Categories")
                .WithSettings(new ContentPickerFieldSettings
                {
                    DisplayedContentTypes = new[] { "Category" },
                    Multiple = true,
                    Required = true
                })));

        // Post
        await definitionManager.AlterTypeDefinitionAsync("Post", type => type
            .DisplayedAs("Post")
            .Creatable()
            .Listable()
            .Draftable()
            .WithPart("TitlePart", part => part
                .WithSettings(new TitlePartSettings
                {
                    RenderTitle = true
                }))
            .WithPart("AutoroutePart", part => part
                .WithSettings(new AutoroutePartSettings
                {
                    Pattern = "{{ ContentItem.DisplayText | slugify }}",
                    ShowHomepageOption = false,
                    AllowUpdatePath = true
                }))
            .WithPart("HtmlBodyPart")
            .WithPart("PostPart"));
    }

    private async Task<List<ContentItem>> SeedItems(string type, int start, int count)
    {
        logger.LogCritical($"Seeding {type}s...");

        var list = new List<ContentItem>();
        for (int i = start; i < start+count; i++)
        {
            var item = await contentManager.NewAsync(type);
            item.Alter<TitlePart>(title => title.Title = $"{type} {i + 1}");
            await contentManager.CreateAsync(item, VersionOptions.Published);
            list.Add(item);
        }

        logger.LogCritical($"Seeded {count} {type}s.");
        return list;
    }

    private async Task SeedPosts(List<ContentItem> tags, List<ContentItem> categories, int start, int count)
    {
        logger.LogCritical("Seeding Posts...");
        var random = new Random();

        for (var i = start; i < start + count; i++)
        {
            var post = await contentManager.NewAsync("Post");
            post.Alter<TitlePart>(title => title.Title = $"Post {i + 1}");
            post.Alter<AutoroutePart>(autoroute => autoroute.Path = $"post-{i + 1}".ToLowerInvariant());
            post.Alter<HtmlBodyPart>(body => body.Html = $"<p>Content for post {i + 1}.</p>");

            // Pick random tags and categories safely
            var tagIds = tags.Any() ? tags.OrderBy(_ => random.Next()).Take(Math.Min(3, tags.Count)).Select(t => t.ContentItemId).ToArray() : Array.Empty<string>();
            var categoryIds = categories.Any() ? categories.OrderBy(_ => random.Next()).Take(Math.Min(2, categories.Count)).Select(c => c.ContentItemId).ToArray() : Array.Empty<string>();

            post.Alter<PostPart>( f => f.Tags.ContentItemIds = tagIds);
            post.Alter<PostPart>( f => f.Categories.ContentItemIds = categoryIds);
           
            await contentManager.CreateAsync(post, VersionOptions.Published);

            if ((i + 1) % 5 == 0)
            {
                logger.LogCritical("Created {Count} posts...", i + 1);
            }
        }

        logger.LogCritical("Seeding Posts complete!");
    }
}