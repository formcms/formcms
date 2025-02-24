var builder = DistributedApplication.CreateBuilder(args);
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.SqlServerCmsApp>("web").WithReference(sqlServer).WaitFor(sqlServer);
builder.Build().Run();