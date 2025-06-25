var builder = DistributedApplication.CreateBuilder(args);
var nats = builder
    .AddNats("nats")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlServer = builder
    .AddSqlServer("sqlserver")
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.SqlServerCmsApp>("web")
    .WithReference(sqlServer).WaitFor(sqlServer)
    .WithReference(nats).WaitFor(nats);

builder.Build().Run();