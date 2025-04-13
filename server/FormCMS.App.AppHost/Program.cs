using FormCMS.App;

var builder = DistributedApplication.CreateBuilder(args);

/*
var nats = builder
    .AddNats(AppConstants.Nats)
    .WithLifetime(ContainerLifetime.Persistent);

var mongoCmsDb = builder
    .AddMongoDB("mongo")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false)
    .AddDatabase(AppConstants.MongoCms);
    */

var postgresCmsDb = builder
    .AddPostgres(AppConstants.PostgresCms)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly:false);

builder.AddProject<Projects.FormCMS_App>("web")
    .WithEnvironment(AppConstants.EnableWebApp,"true")
    .WithArgs(args)
    // .WithReference(nats).WaitFor(nats)
    // .WithReference(mongoCmsDb).WaitFor(mongoCmsDb)
    .WithReference(postgresCmsDb).WaitFor(postgresCmsDb);

// builder.AddProject<Projects.FormCMS_App>("worker")
    // .WithEnvironment(AppConstants.EnableHostApp, "true")
    // .WithArgs(args)
    // .WithReference(nats).WaitFor(nats)
    // .WithReference(mongoCmsDb).WaitFor(mongoCmsDb);
    
builder.Build().Run();