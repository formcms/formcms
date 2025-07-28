using FormCMS.Notify.Models;

namespace FormCMS.Notify.Services;

public interface INotificationCollectService
{
    Task Insert(Notification notification, CancellationToken ct);
}