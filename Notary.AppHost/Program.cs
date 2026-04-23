using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Notary_Web>("portal")
    .WithHttpsHealthCheck();

builder.Build().Run();
