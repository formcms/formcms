using System.Text.Json;

namespace FormCMS.Core.Messaging;

/*
e.g. user user1 comments on user2's post 1, 
{
  userId: user1
  targetUserId: user2
  entityName: post
  recordId: 1
  activity: comment
  operation: create
}
*/
public record ActivityMessage(
    string UserId, 
    string TargetUserId, 
    string EntityName, 
    long RecordId, 
    string Activity,  
    string Operation,
    string Message,
    string Url = ""
);

public static class ActivityMessageExtensions
{
    public static string ToJson(this ActivityMessage activity)
        => JsonSerializer.Serialize(activity);

    public static ActivityMessage ParseJson(string json)
        => JsonSerializer.Deserialize<ActivityMessage>(json)!;
}