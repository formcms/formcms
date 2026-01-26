namespace FormCMS.Core.Descriptors;

public sealed record Page(
    string Name,
    string Title,
    string Html,
    string Source,/* grape.js or ai*/
    string Metadata,
    
    /*for grape.js page*/
    string? Query,
    string Css,
    string Components,
    string Styles);

public record Architecture(
    List<SelectedQuery> SelectedQueries
);

public record SelectedQuery(
    string FieldName,
    string QueryName,
    string Description,
    string Type, //list or single,
    Dictionary<string, string> Args // Values: 'fromPath' | 'fromQuery'
);


public sealed record PageMetadata(Architecture Architecture);

public class PageConstants
{
    public const string Home = "home";
    public const string PageName = "pageName";
    public const string PageSourceAi = "ai";
    public const string PageQueryTypeSingle = "single";
    public const string PageQueryArgFromPath = "fromPath";
}