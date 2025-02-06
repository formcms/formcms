using FluentResults;

namespace FormCMS.Core.Descriptors;

public interface IAttributeValueResolver
{
    public bool ResolveVal(LoadedAttribute attr, string v, out ValidValue? value);
}

public interface IEntityVectorResolver
{
    public Task<Result<AttributeVector>> ResolveVector(LoadedEntity entity, string fieldName, PublicationStatus? status);
}