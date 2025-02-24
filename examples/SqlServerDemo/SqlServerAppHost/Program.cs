var builder = DistributedApplication.CreateBuilder(args);
var sqlServer = builder
    .AddSqlServer("sqlserver")
    .WithEnvironment("TZ", "America/New_York")  //updatedAt, createdAt are using this
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.SqlServerCmsApp>("web").WithReference(sqlServer).WaitFor(sqlServer);
builder.Build().Run();