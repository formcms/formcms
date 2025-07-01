using FormCMS.Core.Descriptors;

namespace FormCMS.Auth.Services;

public interface ISchemaAuthService
{
    void CheckRole();
    Task Delete(Schema schema);
    Task<Schema> BeforeSave(Schema schema);
    Task AfterSave(Schema schema);
}