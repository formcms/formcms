namespace FormCMS.Core.Descriptors;

public sealed record Page(
    string Name,
    string Title,
    string Html,
    PageMetadata? Metadata = null
    );

public record PageArchitecture(
    System.Text.Json.JsonElement? Sections = null,
    List<SelectedQuery>? SelectedQueries = null,
    string? PageTitle = null,
    string? ArchitectureHints = null,
    System.Text.Json.JsonElement? ComponentInstructions = null
);
public record PagePlan(
    string? PageName = null,
    string? PageTitle = null,
    string? EntityName = null,
    string? PageType = null,
    string? PrimaryParameter = null
);

public record SelectedQuery(
    string FieldName,
    string QueryName,
    string Type, //list or single,
    Dictionary<string, string> Args, // Values: 'fromPath' | 'fromQuery'
    string? Description = null
);

public sealed record PageMetadata(
    PageArchitecture? Architecture = null,
    PagePlan? Plan = null,
    bool? EnableVisitTrack = null,
    System.Text.Json.JsonElement? Components = null,
    string? UserInput = null,
    string? TemplateId = null
);

public class PageConstants
{
    public const string Home = "home";
    public const string PageFieldToplist = "topList";
    public const string PageQueryTypeSingle = "single";
    public const string PageQueryArgFromPath = "fromPath";
}