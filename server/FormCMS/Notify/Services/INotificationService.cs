using FormCMS.Utils.DisplayModels;

namespace FormCMS.Notify.Services;

public interface INotificationService
{
    Task<ListResponse> List(StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task<long> UnreadCount(CancellationToken ct);
}