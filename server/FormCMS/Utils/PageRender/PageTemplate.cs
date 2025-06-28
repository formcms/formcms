namespace FormCMS.Utils.PageRender;

public sealed record PageTemplateConfig(string Path);
public sealed class PageTemplate(PageTemplateConfig config)
{
    private readonly string _template = LoadTemplate(config.Path);

    static string LoadTemplate(string templatePath)
    {
        return File.Exists(templatePath)
            ? File.ReadAllText(templatePath)
            : "";
    }

    public string Build(string title, string body, string css)
    {
        return _template.
            Replace("<!--title-->", title).
            Replace("<!--body-->", body).
            Replace("<!--css-->", css);
    }
}