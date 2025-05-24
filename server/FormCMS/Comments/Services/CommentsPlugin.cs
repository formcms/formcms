using FormCMS.Comments.Models;
using FormCMS.Core.Descriptors;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Comments.Services;

public class CommentsPlugin:ICommentPlugin
{
    public Task LoadComments(LoadedEntity missing_name, HashSet<string> fields, IEnumerable<Record> records, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Entity[] ExtendEntities(IEnumerable<Entity> entities)
    {
        var result = new List<Entity>();
        foreach (var e in entities)
        {
            var entity = e with
            {
                Attributes =
                [
                    new Attribute(CommentConstants.EntityName, 
                        Header:"Comment",DataType: DataType.Collection),
                    ..e.Attributes
                ]
            };
            result.Add(entity);
        }
        return result.ToArray();
    }
}