using System.Collections.Immutable;
using FormCMS.Core.Descriptors;

namespace FormCMS.Auth.Services;

public interface IEntityAuthService
{
    ImmutableArray<ValidFilter> ApplyListPermissionFilter(string entityName, LoadedEntity entity, ImmutableArray<ValidFilter> filters);
    Task CheckGetSinglePermission(LoadedEntity entity, ValidValue recordId);
    Task CheckUpdatePermission(LoadedEntity entity, ValidValue recordId);
    Task CheckUpdatePermission(LoadedEntity entity, Record record);
    void CheckInsertPermission(LoadedEntity entity);
    void AssignCreatedBy(Record record);
}