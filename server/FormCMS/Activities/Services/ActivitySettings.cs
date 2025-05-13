namespace FormCMS.Activities.Services;

public record ActivitySettings(
    bool EnableBuffering,
    HashSet<string> ToggleActivities,
    HashSet<string> RecordActivities,
    HashSet<string> AutoRecordActivities,
    Dictionary<string,long> Weights,
    DateTime ReferenceDateTime,
    long HourBoostWeight
);

public static class ActivityServiceExtensions
{
    public static string[] AllCountTypes(this ActivitySettings activitySettings)
        => activitySettings.AutoRecordActivities
            .Concat(activitySettings.ToggleActivities)
            .Concat(activitySettings.RecordActivities)
            .ToArray();
}

