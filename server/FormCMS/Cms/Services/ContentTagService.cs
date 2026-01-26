using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.FileStore;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;

namespace FormCMS.Cms.Services;

public class ContentTagService(
    IQueryService queryService,
    IFileStore fileStore,
    ShardGroup shardGroup

) : IContentTagService
{
    public async Task<ContentTag[]> GetContentTags(LoadedEntity entity, string[] ids, CancellationToken ct)
    {

        var contentFieldName = string.IsNullOrWhiteSpace(entity.ContentTagField)
            ? entity.Attributes.FirstOrDefault(x => x.DisplayType == DisplayType.Editor)
                ?.Field ?? ""
            : entity.ContentTagField;
        
        var titleFieldName = string.IsNullOrWhiteSpace(entity.TitleTagField)
            ?entity.LabelAttribute.Field??""
            : entity.TitleTagField;

        var subtitleFieldName = string.IsNullOrWhiteSpace(entity.SubtitleTagField)
            ? entity.Attributes
                .FirstOrDefault(x => x.DisplayType == DisplayType.Textarea )?.Field ?? ""
            : entity.SubtitleTagField;
        
        var publishedAtFieldName =string.IsNullOrWhiteSpace(entity.PublishTimeTagField)
            ? DefaultAttributeNames.PublishedAt.Camelize()
            : entity.PublishTimeTagField;

        var imageField = string.IsNullOrWhiteSpace(entity.ImageTagField)
            ?entity.Attributes.FirstOrDefault(x => x.DisplayType == DisplayType.Image)?.Field ?? ""
            :entity.ImageTagField;
        
        
        if (string.IsNullOrWhiteSpace(entity.TagsQuery))
        {
            var idValues = new List<ValidValue>();
            foreach (var id in ids)
            {
                if (entity.PrimaryKeyAttribute.ResolveVal(id, out var idValue) && idValue is not null)
                {
                    idValues.Add(idValue.Value!);
                }
            }

            var fields = new List<string>
            {
                entity.PrimaryKey,
                contentFieldName,
                titleFieldName,
                subtitleFieldName,
                imageField,
                publishedAtFieldName
            }.Where(x => !string.IsNullOrWhiteSpace(x));
            
            var query = entity.ByIdsQuery(fields, idValues, PublicationStatus.Published);
            var records = await shardGroup.ReplicaDao.Many(query, ct);

            var tags = records.Select(record => new ContentTag(Data: record,
                RecordId: record.StrOrEmpty(entity.PrimaryKey),
                Title: Trim(record.StrOrEmpty(titleFieldName)),
                Content: Trim(record.StrOrEmpty(contentFieldName)), 
                Subtitle: Trim(record.StrOrEmpty(subtitleFieldName)),
                Url: $"/{entity.Name}/{record.StrOrEmpty(entity.PrimaryKey)}", 
                Image: fileStore.GetUrl(record.StrOrEmpty(imageField)),
                PublishedAt: DateTime.Parse(record.StrOrEmpty(publishedAtFieldName))));
            return tags.ToArray();
        }
        else
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
                    Data: record,
                    RecordId: id,
                    Url: (record.ByJsonPath<string>(entity.PageUrl, out var val) ? val! : entity.PageUrl) + id,
                    Content: record.ByJsonPath<string>(entity.ContentTagField, out var content) ? content! : "",
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
        }
        string Trim(string? s) => s?.Length > 255 ? s[..255] : s ?? "";
    }
}