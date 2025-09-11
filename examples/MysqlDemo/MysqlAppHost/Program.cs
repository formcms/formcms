var builder = DistributedApplication.CreateBuilder(args);
var nats = builder
    .AddNats("nats")
    .WithLifetime(ContainerLifetime.Persistent);

var mysql = builder
    .AddMySql("mysql")
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.MysqlWebApp>("web")
    .WithReference(nats).WaitFor(nats)
    .WithReference(mysql).WaitFor(mysql);
builder.Build().Run();