using FluentResults;

namespace FormCMS.Core.Descriptors;

public interface IEntityVectorResolver
{
    public Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName, PublicationStatus? status);
}