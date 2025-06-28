using FormCMS.App;

var webApp = await WebApp.Build(args);
var hostApp = WorkerApp.Build(args);
await Task.WhenAll(webApp?.RunAsync() ?? Task.CompletedTask, hostApp?.RunAsync() ?? Task.CompletedTask);