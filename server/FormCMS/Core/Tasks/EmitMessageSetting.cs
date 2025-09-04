using System.Text.Json;

namespace FormCMS.Core.Tasks;
public record EmitMessageSetting(string EntityName);

public static class EmitMessageSettingHelper
{
    public static string ToJson(this EmitMessageSetting setting)
    {
        return JsonSerializer.Serialize(setting);
    }

    public static EmitMessageSetting? Parse(string json)
    {
        return JsonSerializer.Deserialize<EmitMessageSetting>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}