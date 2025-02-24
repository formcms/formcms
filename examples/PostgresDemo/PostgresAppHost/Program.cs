var builder = DistributedApplication.CreateBuilder(args);
var postgres = builder
    .AddPostgres("postgres")
    .WithEnvironment("TZ", "America/New_York")  //updatedAt, createdAt are using this
    .WithDataVolume(isReadOnly:false)
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.PostgresWebApp>("web").WithReference(postgres).WaitFor(postgres);

builder.Build().Run();