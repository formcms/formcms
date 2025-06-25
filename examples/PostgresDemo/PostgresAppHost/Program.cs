var builder = DistributedApplication.CreateBuilder(args);
var nats = builder
    .AddNats("nats")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.PostgresWebApp>("web")
    .WithReference(nats).WaitFor(nats)
    .WithReference(postgres).WaitFor(postgres);


builder.Build().Run();