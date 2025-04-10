namespace FormCMS.Infrastructure.Buffers;

public class BufferSettings
{
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(5);
}