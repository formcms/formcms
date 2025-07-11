using FormCMS.Cms.Services;

namespace FormCMS.Cms.Handlers;

public static class ChunkUploadHandler
{
    public static void MapChunkUploadHandler(this RouteGroupBuilder app)
    {
        app.MapPost(
            "/",
            async (
                HttpContext context,
                IChunkUploadService service,
                CancellationToken ct
            ) =>
            {
                var fileId = context.Request.Form["fileId"].ToString();
                // var fileName = context.Request.Form["fileName"].ToString();
                var chunkNumber = int.Parse(context.Request.Form["chunkNumber"]!);
                var file = context.Request.Form.Files[0];
                await service.UploadChunk(fileId, chunkNumber, file, ct);
            }
        );

        app.MapGet(
            "/status",
            async (
                IChunkUploadService service,
                string fileName,
                long size,
                CancellationToken ct
            ) => await service.ChunkStatus(fileName, size, ct)
        );

        app.MapPost(
            "/commit",
            async (
                IChunkUploadService service,
                HttpContext context,
                CancellationToken ct
            ) =>
            {
                var path = context.Request.Form["fileId"].ToString();
                var fileName = context.Request.Form["fileName"].ToString();
                await service.Commit(path, fileName, ct);
            }
        );
    }
}