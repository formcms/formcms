namespace FormCMS.Cms.Services;

public interface IAdminPanelSchemaService
{
    Task<IResult> GetMenu(string name, CancellationToken ct);
    Task<IResult> GetEntity(string name, CancellationToken ct);
}
