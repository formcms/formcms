var builder = DistributedApplication.CreateBuilder(args);
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);
builder.AddProject<Projects.PostgresWebApp>("web").WithReference(postgres).WaitFor(postgres);

builder.Build().Run();