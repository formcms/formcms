using FormCMS.Core.Descriptors;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Cms.Services;

public class EntityLinkService(
    IQueryService queryService
    ):IEntityLinkService
{
    public async Task<Link[]> GetLinks( LoadedEntity entity, string[] ids, CancellationToken ct)
    {
        var strAgs = new StrArgs
        {
            [entity.BookmarkQueryParamName] = ids.Select(x => x.ToString()).ToArray()
        };
        var records = await queryService.ListWithAction(entity.BookmarkQuery, new Span(), new Pagination(), strAgs, ct);
        return records.Select(GetLink).ToArray();

        Link GetLink(Record record)
        {
            var id = record.StrOrEmpty(entity.PrimaryKey);
            return new Link(
                RecordId: id,
                Url: entity.PageUrl + id,
                Title: record.ByJsonPath<string>(entity.BookmarkTitleField, out var title) ? Trim(title!) : "",
                Image: record.ByJsonPath<string>(entity.BookmarkImageField, out var image) ? Trim(image!) : "",
                Subtitle: record.ByJsonPath<string>(entity.BookmarkSubtitleField, out var subtitle)
                    ? Trim(subtitle!)
                    : "",
                PublishedAt: record.ByJsonPath<DateTime>(entity.BookmarkPublishTimeField, out var publishTime)
                    ? publishTime
                    : null
            );
        }

        string Trim(string? s) => s?.Length > 255 ? s[..255] : s ?? "";
    }
}