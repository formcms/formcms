using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Services;

public interface IAdminPanelSchemaService
{
    Task<IResult> GetMenu(string name, CancellationToken ct ); 
    Task<IResult> GetEntity(string name, CancellationToken ct ); 
}
