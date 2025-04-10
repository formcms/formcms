namespace FormCMS.Infrastructure.Buffers;

public interface IStatusBuffer
{
    
    // Checks if a user has performed an activity (e.g., liked, saved, viewed) on a record
    Task<bool> Get(string userId, string recordId, Func<Task<bool>> getStatusAsync);

    // Toggles the user's activity status (e.g., like to unlike, or vice versa) and returns status change 
    Task<bool> Toggle(string userId, string recordId, bool isActive,Func<Task<bool>> getStatusAsync);

    // Records an activity as performed (e.g., view) 
    Task Set(string userId, string recordId);

    Task<(string, string, bool)[]> GetAfterLastFlush(DateTime lastFlushTime);
}