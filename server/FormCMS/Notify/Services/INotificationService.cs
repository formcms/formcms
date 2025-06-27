using FormCMS.Utils.DisplayModels;

namespace FormCMS.Notify.Services;

public interface INotificationService
{
    Task EnsureNotificationTables();
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task<int> UnreadCount(CancellationToken ct);
}