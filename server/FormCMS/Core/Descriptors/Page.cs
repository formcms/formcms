namespace FormCMS.Core.Descriptors;

public sealed record Page(
    string Name,
    string Title,
    string Html,
    PageMetadata? Metadata = null
    );

public record PageArchitecture(
    List<SelectedQuery> SelectedQueries
);
public record PagePlan(string EntityName);

public record SelectedQuery(
    string FieldName,
    string QueryName,
    string Type, //list or single,
    Dictionary<string, string> Args // Values: 'fromPath' | 'fromQuery'
);

public sealed record PageMetadata(
    PageArchitecture Architecture, 
    PagePlan Plan, 
    bool EnableTopList,
    bool EnableEngagementBar = false,
    bool EnableUserAvatar = false,
    bool EnableVisitTrack = false,
    System.Text.Json.JsonElement? LayoutJson = null,
    System.Text.Json.JsonElement? ComponentInstructions = null,
    System.Text.Json.JsonElement? Components = null
);

public class PageConstants
{
    public const string Home = "home";
    public const string PageFieldToplist = "topList";
    public const string PageSourceAi = "ai";
    public const string PageQueryTypeSingle = "single";
    public const string PageQueryArgFromPath = "fromPath";
}