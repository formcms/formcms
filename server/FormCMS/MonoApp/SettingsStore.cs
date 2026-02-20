namespace FormCMS.MonoApp;

using System.Text.Json;
using System.Text.Json.Nodes;

public  class SettingsStore(string path)
{
    private  string FileName => Path.Combine(path , "formcms.settings.json");

    public void Save(MonoSettings monoSettings)
    {
        var root = new JsonObject
        {
            ["FormCms"] = JsonSerializer.SerializeToNode(monoSettings)
        };

        File.WriteAllText(
            FileName,
            root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
    }

    public MonoSettings? Load()
    {
        if (!File.Exists(FileName))
            return null;

        var json = JsonNode.Parse(File.ReadAllText(FileName));
        return json?["FormCms"]?.Deserialize<MonoSettings>();
    }
}
