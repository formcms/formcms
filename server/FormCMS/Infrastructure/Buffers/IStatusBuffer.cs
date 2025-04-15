namespace FormCMS.Infrastructure.Buffers;

public interface IStatusBuffer
{
    // Checks if a user has performed activities (e.g., liked, saved, viewed) on a record
    Task<Dictionary<string,bool>> Get(string[] keys, Func<string,Task<bool>> getStatus);

    // Toggles the user's activity status (e.g., like to unlike, or vice versa) and returns status change 
    Task<bool> Toggle(string key, bool isActive,Func<string,Task<bool>> getStatus);

    // Records activities as performed (e.g., view) 
    Task Set(string[] keys );

    Task<Dictionary<string,bool>> GetAfterLastFlush(DateTime lastFlushTime);
}