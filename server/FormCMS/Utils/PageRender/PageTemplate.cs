using FormCMS.Utils.ResultExt;

namespace FormCMS.Utils.PageRender;

public sealed class PageTemplate(string mainPage, string subsPage)
{
    private readonly string _mainPage = PageTemplateHelper.LoadTemplate(mainPage);
    private readonly string _subsPage = PageTemplateHelper.LoadTemplate(subsPage);

    public string BuildMainPage(string title, string body, string css)
        => _mainPage.Replace("<!--title-->", title).Replace("<!--body-->", body).Replace("<!--css-->", css);

    public string BuildSubsPage(string url)
        => _subsPage.Replace("{{url}}", url);
}

public static class PageTemplateHelper
{
    public static string LoadTemplate(string templatePath)
        => File.Exists(templatePath)
            ? File.ReadAllText(templatePath)
            : throw new ResultException("Template not found");
}