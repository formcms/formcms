using System.Security.Claims;
using System.Text.Json;
using FormCMS.Auth.Services;
using FormCMS.Cms.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Assets;
using FormCMS.Core.Identities;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.Cms.Workers;

public sealed class AssetUpdateMessageHandler(
    ILogger<AssetUpdateMessageHandler> logger,
    IStringMessageConsumer consumer,
    IServiceProvider provider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("AssetUpdateMessageHandler starting at: {time}", DateTimeOffset.Now);

        await consumer.Subscribe(
            AssetTopics.AssetUpdate,
            "AssetUpdateMessageHandler",
            async s =>
            {
                try
                {
                    var msg = JsonSerializer.Deserialize<AssetUpdateMessage>(s);
                    if (msg is null)
                    {
                        logger.LogWarning("Could not deserialize AssetUpdateMessage: {Raw}", s);
                        return;
                    }

                    await using var scope = provider.CreateAsyncScope();
                    var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
                    var accountService = scope.ServiceProvider.GetService<IAccountService>();

                    HttpContext? originalContext = null;
                    if (httpContextAccessor != null && accountService != null && !string.IsNullOrEmpty(msg.UserId))
                    {
                        originalContext = httpContextAccessor.HttpContext;

                        // Fetch the user from the database to get all roles/claims
                        var userResult = await accountService.InternalGetSingleUser(msg.UserId, ct);
                        var claims = userResult.ToClaims();

                        var identity = new ClaimsIdentity(claims, "QueueImpersonation");
                        var principal = new ClaimsPrincipal(identity);

                        httpContextAccessor.HttpContext = new DefaultHttpContext
                        {
                            User = principal
                        };
                    }

                    try
                    {
                        var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();

                        if (msg.IsNewAsset)
                        {
                            await assetService.CreateNewAssetRefOriginal(msg.OriginalPath, msg.NewPath!, ct);
                        }

                        var asset = new Asset(
                            Path: msg.OriginalPath,
                            Url: msg.NewUrl,
                            Progress: msg.Progress
                        );
                        await assetService.UpdateConvertProgress(asset, ct);
                        logger.LogInformation("AssetUpdateMessageHandler processed: {Path}", msg.OriginalPath);
                    }
                    finally
                    {
                        if (httpContextAccessor != null && !string.IsNullOrEmpty(msg.UserId))
                        {
                            httpContextAccessor.HttpContext = originalContext;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing AssetUpdate message");
                }
            },
            ct
        );
    }
}
