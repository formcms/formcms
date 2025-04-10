namespace FormCMS.Utils.DateTimeExt;


// Extension method to truncate to minute
public static class DateTimeExtensions
{
    public static DateTime TruncateToMinute(this DateTime dt) =>
        new (dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
}