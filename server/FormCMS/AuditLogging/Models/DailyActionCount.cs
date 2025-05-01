namespace FormCMS.AuditLogging.Models;

public record DailyActionCount(ActionType Action, DateTime Day, long Count);
