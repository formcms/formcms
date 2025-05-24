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
using FormCMS.Utils.DisplayModels;
using NJsonSchema.Generation;

//shared
TsGenerator.GenerateCode<XEntity>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/xEntity.ts");
TsGenerator.GenerateCode<ListResponse>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/listResponse.ts");
TsGenerator.GenerateCode<ListResponseMode>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/listResponseMode.ts");

//audit log
TsGenerator.GenerateCode<AuditLog>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auditLog/types/auditLog.ts");

//cms
TsGenerator.GenerateCode<Asset>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/asset.ts");
TsGenerator.GenerateCode<AssetLink>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/assetLink.ts");
TsGenerator.GenerateCode<LookupListResponse>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/lookupListResponse.ts");
TsGenerator.GenerateCode<DefaultAttributeNames>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/defaultAttributeNames.ts");
TsGenerator.GenerateCode<DefaultColumnNames>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/defaultColumnNames.ts");
TsGenerator.GenerateCode<PublicationStatus>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/publicationStatus.ts");
TsGenerator.GenerateCode<SpecialQueryKeys>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/specialQueryKeys.ts");
TsGenerator.GenerateCode<SystemTask>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/systemTask.ts");

//auth
TsGenerator.GenerateCode<Menu>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/menu.ts");
TsGenerator.GenerateCode<ProfileHandler.ChangePasswordReq>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/changePasswordReq.ts");
TsGenerator.GenerateCode<LoginHandler.RegisterReq>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/registerReq.ts");
TsGenerator.GenerateCode<LoginHandler.LoginReq>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/loginReq.ts");
TsGenerator.GenerateCode<UserAccess>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/userAccess.ts");
TsGenerator.GenerateCode<RoleAccess>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/roleAccess.ts");

//activity
TsGenerator.GenerateCode<DailyActionCount>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auditLog/types/dailyActionCount.ts");
TsGenerator.GenerateCode<Activity>("../../../FormCmsPortal/libs/FormCmsAdminSdk/activity/types/activity.ts");
TsGenerator.GenerateCode<Bookmark>("../../../FormCmsPortal/libs/FormCmsAdminSdk/activity/types/bookmark.ts");
TsGenerator.GenerateCode<BookmarkFolder>("../../../FormCmsPortal/libs/FormCmsAdminSdk/activity/types/bookmarkFolder.ts");
TsGenerator.GenerateCode<DailyActivityCount>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/activity/types/dailyActivityCount.ts");
TsGenerator.GenerateCode<PageVisitCount>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/activity/types/pageVisitCount.ts");

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
            SerializerOptions =CamelNaming
        };
        
        var schema = JsonSchema.FromType<T>(jsonSettings);
        var typeScriptGeneratorSettings = new TypeScriptGeneratorSettings
        {
            TypeStyle = TypeScriptTypeStyle.Interface, 
            TypeScriptVersion = 4.3m

        };
        var generator = new TypeScriptGenerator(schema,typeScriptGeneratorSettings);
        var src = generator.GenerateFile();
        File.WriteAllText(fileName, src);
    }
}