using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Notary_Web>("portal")
    .WithHttpHealthCheck();

builder.Build().Run();
