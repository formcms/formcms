using FormCMS.Cms.Services;
using FormCMS.Core.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace FormCMS.Cms.Handlers;

public static class TaskHandler
{
    public static void MapTasksHandler(this RouteGroupBuilder builder)
    {
        builder.MapGet("/", (
            ITaskService s, 
            HttpContext context,
            int? offset, 
            int? limit, 
            CancellationToken ct
        ) => s.List(QueryHelpers.ParseQuery(context.Request.QueryString.Value), offset, limit, ct));

        builder.MapGet("/entity", (ITaskService s) => s.GetEntity());    
        
        //export
        builder.MapPost("/export", (ITaskService s) => s.AddExportTask());
        
        builder.MapGet("/export/download/{id:int}", async (
            HttpContext context,
            ITaskService s, 
            int id,
            CancellationToken ct
        ) => context.Response.Redirect(await s.GetTaskFileUrl(id,ct)));

        builder.MapPost("/export/archive/{id:int}", (
            ITaskService s,
            int id,
            CancellationToken ct
        ) => s.DeleteTaskFile(id,ct));

        //import
        builder.MapPost($"/import/", (
            HttpContext context,
            ITaskService s
        ) =>s.AddImportTask(context.Request.Form.Files[0]));

        builder.MapPost($"/import/demo", (
            HttpContext context,
            ITaskService s
        ) => s.ImportDemoData());
        
        //emit 
        builder.MapPost($"/emit/", (
            ITaskService s,
            EmitMessageSetting setting
        ) =>s.AddEmitMessageTask(setting));
       
    }
}