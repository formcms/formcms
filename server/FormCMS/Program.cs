using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Activities.Models;
using FormCMS.AuditLogging.Models;
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

TsGenerator.GenerateCode<XEntity>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/xEntity.ts");
TsGenerator.GenerateCode<ListResponse>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/listResponse.ts");
TsGenerator.GenerateCode<ListResponseMode>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/types/listResponseMode.ts");

TsGenerator.GenerateCode<AuditLog>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auditLog/types/auditLog.ts");

TsGenerator.GenerateCode<Asset>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/asset.ts");
TsGenerator.GenerateCode<AssetLink>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/assetLink.ts");
TsGenerator.GenerateCode<LookupListResponse>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/lookupListResponse.ts");
TsGenerator.GenerateCode<DefaultAttributeNames>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/defaultAttributeNames.ts");
TsGenerator.GenerateCode<DefaultColumnNames>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/defaultColumnNames.ts");
TsGenerator.GenerateCode<PublicationStatus>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/publicationStatus.ts");
TsGenerator.GenerateCode<SpecialQueryKeys>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/specialQueryKeys.ts");
TsGenerator.GenerateCode<SystemTask>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/cms/types/systemTask.ts");


TsGenerator.GenerateCode<Menu>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/menu.ts");
TsGenerator.GenerateCode<ProfileDto>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/profileDto.ts");
TsGenerator.GenerateCode<UserAccess>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/userAccess.ts");
TsGenerator.GenerateCode<RoleAccess>("../../../FormCmsAdminApp/libs/FormCmsAdminSdk/auth/types/roleAccess.ts");

TsGenerator.GenerateCode<Activity>("../../../FormCmsPortal/libs/FormCmsAdminSdk/activity/types/activity.ts");

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