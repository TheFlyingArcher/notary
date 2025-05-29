using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("notary-mongo", 27017)
    .WithMongoExpress()
    .WithLifetime(ContainerLifetime.Persistent);
var mongodb = mongo.AddDatabase("notary");

builder.AddProject<Notary_Web>("portal")
    .WithHttpsHealthCheck()
    .WaitFor(mongodb);

builder.Build().Run();
