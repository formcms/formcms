var builder = DistributedApplication.CreateBuilder(args);
var nats = builder
    .AddNats("nats")
    .WithLifetime(ContainerLifetime.Persistent);

// not find out how to enable full text search
// to use full text search feature, can build a customize sql server with fts image using the docker file
// formcms/etc/sqlserver-fts/.Dockerfile
var sqlServer = builder
    .AddSqlServer("sqlserver")
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.SqlServerCmsApp>("web")
    .WithReference(sqlServer).WaitFor(sqlServer)
    .WithReference(nats).WaitFor(nats);

builder.Build().Run();