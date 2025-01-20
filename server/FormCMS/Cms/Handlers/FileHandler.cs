using FormCMS.Infrastructure.LocalFileStore;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Cms.Handlers;

public static class FileHandler
{
    public static void MapFileHandlers(this RouteGroupBuilder app)
    {
        app.MapPost($"/", async (
            IFileStore store, HttpContext context
        ) => string.Join(",", (await store.Save(context.Request.Form.Files)).Ok()));
    }
}