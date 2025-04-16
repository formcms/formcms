namespace FormCMS.Utils.LoggerExt;

public static class LoggerExtensions
{
    public static object LogInformationEx(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogInformation(message, args);
        return new {};
    }
}