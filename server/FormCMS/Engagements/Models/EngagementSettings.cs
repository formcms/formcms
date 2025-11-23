namespace FormCMS.Engagements.Models;

public record EngagementSettings(
    bool EnableBuffering,
    HashSet<string> CommandToggleActivities,
    HashSet<string> CommandRecordActivities,
    HashSet<string> CommandAutoRecordActivities,
    HashSet<string> EventRecordActivities,
    Dictionary<string,long> Weights,
    DateTime ReferenceDateTime,
    long HourBoostWeight
);

public static class EngagementSettingsExtensions
{
    public static HashSet<string> AllCountTypes(this EngagementSettings engagementSettings)
        => engagementSettings.CommandAutoRecordActivities
            .Concat(engagementSettings.CommandToggleActivities)
            .Concat(engagementSettings.CommandRecordActivities)
            .Concat(engagementSettings.EventRecordActivities)
            .ToHashSet();

    public static readonly EngagementSettings DefaultEngagementSettings = new (
        EnableBuffering: true,
        CommandToggleActivities: ["like"],
        CommandRecordActivities: ["share"],
        CommandAutoRecordActivities: ["view"],
        Weights: new Dictionary<string, long>
        {
            { "view", 10 },
            { "like", 20 },
            { "share", 30 },
            { "comment", 50 },
        },
        EventRecordActivities: ["comment"],
        ReferenceDateTime: new DateTime(2025, 1, 1),
        HourBoostWeight: 10
    );
}


