using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.RelationDbDao;
using Humanizer;

namespace FormCMS.Cms.Builders;

public static class DatabaseMigratorExtensions
{
    public static async Task EnsureCmsTables(this IPrimaryDao migrator)
    {
        await migrator.MigrateTable(SchemaHelper.TableName, SchemaHelper.Columns);
        await migrator.MigrateTable(TaskHelper.TableName,TaskHelper.Columns);
            
        await migrator.MigrateTable(Assets.TableName, Assets.Columns);
        await migrator.MigrateTable(AssetLinks.TableName, AssetLinks.Columns);
        await migrator.CreateIndex( Assets.TableName, [nameof(Asset.Path).Camelize()], true, CancellationToken.None);
        await migrator.CreateForeignKey(
            AssetLinks.TableName, nameof(AssetLink.AssetId).Camelize(),
            Assets.TableName, nameof(Asset.Id).Camelize(),
            CancellationToken.None);
            
        await migrator.MigrateTable(UploadSessions.TableName, UploadSessions.Columns);
        await migrator.CreateIndex(
            UploadSessions.TableName, 
            [
                nameof(UploadSession.ClientId).Camelize(),
                nameof(UploadSession.FileName).Camelize(),
                nameof(UploadSession.FileSize).Camelize()
            ], 
            false, 
            CancellationToken.None);
    }
    
}