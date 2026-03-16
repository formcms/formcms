namespace FormCMS.Video.Workers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVideoWorkers(this IServiceCollection services)
    {
        services.AddScoped<IConversionStrategy, HlsConversionStrategy>();
        services.AddScoped<IConversionStrategy, Mp3ConversionStrategy>();
        services.AddHostedService<FFMpegWorker>();
        return services;
    }
}
