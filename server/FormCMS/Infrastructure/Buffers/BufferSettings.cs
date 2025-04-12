namespace FormCMS.Infrastructure.Buffers;

public class BufferSettings
{
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);
}