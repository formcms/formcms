namespace FormCMS.Activities.Models;

public record ActivitySettings(
    bool EnableBuffering,
    HashSet<string> CommandToggleActivities,
    HashSet<string> CommandRecordActivities,
    HashSet<string> CommandAutoRecordActivities,
    HashSet<string> EventRecordActivities,
    Dictionary<string,long> Weights,
    DateTime ReferenceDateTime,
    long HourBoostWeight
);

public static class ActivitySettingsExtensions
{
    public static HashSet<string> AllCountTypes(this ActivitySettings activitySettings)
        => activitySettings.CommandAutoRecordActivities
            .Concat(activitySettings.CommandToggleActivities)
            .Concat(activitySettings.CommandRecordActivities)
            .Concat(activitySettings.EventRecordActivities)
            .ToHashSet();

    public static readonly ActivitySettings DefaultActivitySettings = new ActivitySettings(
        EnableBuffering: true,
        CommandToggleActivities: ["like"],
        CommandRecordActivities: ["share"],
        CommandAutoRecordActivities: ["view"],
        Weights: new Dictionary<string, long>
        {
            { "view", 10 },
            { "like", 20 },
            { "share", 30 },
        },
        EventRecordActivities: ["comment"],
        ReferenceDateTime: new DateTime(2025, 1, 1),
        HourBoostWeight: 10
    );
}


