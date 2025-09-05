using FormCMS.Core.Descriptors;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Cms.Services;

public class ContentTagService(
    IQueryService queryService
    ):IContentTagService
{
    public async Task<ContentTag[]> GetContentTags(LoadedEntity entity, string[] ids, CancellationToken ct)
    {
        var strAgs = new StrArgs
        {
            [entity.TagsQueryParam] = ids.Select(x => x.ToString()).ToArray()
        };
        var records = await queryService.ListWithAction(entity.TagsQuery, new Span(), new Pagination(), strAgs, ct);
        return records.Select(GetTags).ToArray();

        ContentTag GetTags(Record record)
        {
            var id = record.StrOrEmpty(entity.PrimaryKey);
            return new ContentTag(
                Data:record,
                RecordId: id,
                Url: (record.ByJsonPath<string>(entity.PageUrl,out var val)?val!:entity.PageUrl) + id,
                Content: record.ByJsonPath<string>(entity.ContentTagField, out var content) ?content! : "",
                Title: record.ByJsonPath<string>(entity.TitleTagField, out var title) ? Trim(title!) : "",
                Image: record.ByJsonPath<string>(entity.ImageTagField, out var image) ? Trim(image!) : "",
                Subtitle: record.ByJsonPath<string>(entity.SubtitleTagField, out var subtitle)
                    ? Trim(subtitle!)
                    : "",
                PublishedAt: record.ByJsonPath<DateTime>(entity.PublishTimeTagField, out var publishTime)
                    ? publishTime
                    : null
            );
        }
        string Trim(string? s) => s?.Length > 255 ? s[..255] : s ?? "";
    }
}