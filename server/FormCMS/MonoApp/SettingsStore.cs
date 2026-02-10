namespace FormCMS.Builders;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class SettingsStore
{
    private const string FileName = "formcms.settings.json";

    public static void Save(Settings settings)
    {
        var root = new JsonObject
        {
            ["FormCms"] = JsonSerializer.SerializeToNode(settings)
        };

        File.WriteAllText(
            FileName,
            root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
    }

    public static Settings? Load()
    {
        if (!File.Exists(FileName))
            return null;

        var json = JsonNode.Parse(File.ReadAllText(FileName));
        return json?["FormCms"]?.Deserialize<Settings>();
    }
}
