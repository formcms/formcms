using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Activities.Models;
using FormCMS.AuditLogging.Models;
using FormCMS.Auth.Handlers;
using FormCMS.Auth.Services;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.Core.Identities;
using FormCMS.Core.Tasks;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Notify.Models;
using FormCMS.Utils.DisplayModels;
using NJsonSchema.Generation;

var apps = new string[] { "FormCmsAdminApp", "FormCmsPortal" };
foreach (var app in apps)
{
//shared
    TsGenerator.GenerateCode<XEntity>($"../../../{app}/libs/FormCmsAdminSdk/types/xEntity.ts");
    TsGenerator.GenerateCode<ListResponse>($"../../../{app}/libs/FormCmsAdminSdk/types/listResponse.ts");
    TsGenerator.GenerateCode<ListResponseMode>(
        $"../../../{app}/libs/FormCmsAdminSdk/types/listResponseMode.ts");

//audit log
    TsGenerator.GenerateCode<AuditLog>($"../../../{app}/libs/FormCmsAdminSdk/auditLog/types/auditLog.ts");

//cms
    TsGenerator.GenerateCode<Asset>($"../../../{app}/libs/FormCmsAdminSdk/cms/types/asset.ts");
    TsGenerator.GenerateCode<AssetLink>($"../../../{app}/libs/FormCmsAdminSdk/cms/types/assetLink.ts");
    TsGenerator.GenerateCode<LookupListResponse>(
        $"../../../{app}/libs/FormCmsAdminSdk/cms/types/lookupListResponse.ts");
    TsGenerator.GenerateCode<DefaultAttributeNames>(
        $"../../../{app}/libs/FormCmsAdminSdk/cms/types/defaultAttributeNames.ts");
    TsGenerator.GenerateCode<DefaultColumnNames>(
        $"../../../{app}/libs/FormCmsAdminSdk/cms/types/defaultColumnNames.ts");
    TsGenerator.GenerateCode<PublicationStatus>(
        $"../../../{app}/libs/FormCmsAdminSdk/cms/types/publicationStatus.ts");
    TsGenerator.GenerateCode<SpecialQueryKeys>(
        $"../../../{app}/libs/FormCmsAdminSdk/cms/types/specialQueryKeys.ts");
    TsGenerator.GenerateCode<SystemTask>($"../../../{app}/libs/FormCmsAdminSdk/cms/types/systemTask.ts");

//auth
    TsGenerator.GenerateCode<Menu>($"../../../{app}/libs/FormCmsAdminSdk/auth/types/menu.ts");
    TsGenerator.GenerateCode<ProfileHandler.ChangePasswordReq>(
        $"../../../{app}/libs/FormCmsAdminSdk/auth/types/changePasswordReq.ts");
    TsGenerator.GenerateCode<LoginHandler.RegisterReq>(
        $"../../../{app}/libs/FormCmsAdminSdk/auth/types/registerReq.ts");
    TsGenerator.GenerateCode<LoginHandler.LoginReq>(
        $"../../../{app}/libs/FormCmsAdminSdk/auth/types/loginReq.ts");
    TsGenerator.GenerateCode<UserAccess>($"../../../{app}/libs/FormCmsAdminSdk/auth/types/userAccess.ts");
    TsGenerator.GenerateCode<RoleAccess>($"../../../{app}/libs/FormCmsAdminSdk/auth/types/roleAccess.ts");

    //activity
    TsGenerator.GenerateCode<DailyActionCount>(
        $"../../../{app}/libs/FormCmsAdminSdk/auditLog/types/dailyActionCount.ts");
    TsGenerator.GenerateCode<Activity>($"../../../{app}/libs/FormCmsAdminSdk/activity/types/activity.ts");
    TsGenerator.GenerateCode<Bookmark>($"../../../{app}/libs/FormCmsAdminSdk/activity/types/bookmark.ts");
    TsGenerator.GenerateCode<BookmarkFolder>(
        $"../../../{app}/libs/FormCmsAdminSdk/activity/types/bookmarkFolder.ts");
    TsGenerator.GenerateCode<DailyActivityCount>(
        $"../../../{app}/libs/FormCmsAdminSdk/activity/types/dailyActivityCount.ts");
    TsGenerator.GenerateCode<PageVisitCount>(
        $"../../../{app}/libs/FormCmsAdminSdk/activity/types/pageVisitCount.ts");
    TsGenerator.GenerateCode<Notification>(
        $"../../../{app}/libs/FormCmsAdminSdk/notifications/types/notification.ts");
}

internal static class TsGenerator
{
    private static readonly JsonSerializerOptions CamelNaming = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static void GenerateCode<T>(string fileName, SystemTextJsonSchemaGeneratorSettings? jsonSettings = null)
    {
        jsonSettings ??= new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = CamelNaming
        };

        var schema = JsonSchema.FromType<T>(jsonSettings);
        var typeScriptGeneratorSettings = new TypeScriptGeneratorSettings
        {
            TypeStyle = TypeScriptTypeStyle.Interface,
            TypeScriptVersion = 4.3m
        };
        var generator = new TypeScriptGenerator(schema, typeScriptGeneratorSettings);
        var src = generator.GenerateFile();
        File.WriteAllText(fileName, src);
    }
}