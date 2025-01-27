using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.AuditLogging.Models;
using FormCMS.Auth.DTO;
using FormCMS.Auth.Services;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;
using NJsonSchema.Generation;

TsGenerator.GenerateCode<XEntity>("../../admin-panel/src/cms-client/types/xEntity.ts");
TsGenerator.GenerateCode<ListResponse>("../../admin-panel/src/cms-client/types/listResponse.ts");
TsGenerator.GenerateCode<ListResponseMode>("../../admin-panel/src/cms-client/types/listResponseMode.ts");
TsGenerator.GenerateCode<LookupListResponse>("../../admin-panel/src/cms-client/types/lookupListResponse.ts");
TsGenerator.GenerateCode<DefaultAttributeNames>("../../admin-panel/src/cms-client/types/defaultAttributeNames.ts");
TsGenerator.GenerateCode<PublicationStatus>("../../admin-panel/src/cms-client/types/publicationStatus.ts");

TsGenerator.GenerateCode<RoleDto>("../../admin-panel/src/auth/types/roleDto.ts");
TsGenerator.GenerateCode<UserDto>("../../admin-panel/src/auth/types/userDto.ts");
TsGenerator.GenerateCode<ProfileDto>("../../admin-panel/src/auth/types/profileDto.ts");
TsGenerator.GenerateCode<Menu>("../../admin-panel/src/auth/types/menu.ts");

TsGenerator.GenerateCode<AuditLog>("../../admin-panel/src/auditLog/types/auditLog.ts");
TsGenerator.GenerateCode<XEntity>("../../admin-panel/src/auditLog/types/xEntity.ts");
TsGenerator.GenerateCode<ListResponse>("../../admin-panel/src/auditLog/types/listResponse.ts");

TsGenerator.GenerateCode<XEntity>("../../admin-panel/src/components/dataTable/xEntity.ts");
TsGenerator.GenerateCode<ListResponse>("../../admin-panel/src/components/dataTable/listResponse.ts");
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