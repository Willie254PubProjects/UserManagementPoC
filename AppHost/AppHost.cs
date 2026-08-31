var builder = DistributedApplication.CreateBuilder(args);
var identity = builder.AddProject<Projects.Identity>("usermanagementpoc-identity");
builder.AddProject<Projects.UserManagementAdmin>("usermanagementpoc-usermanagementadmin")
    .WithReference(identity)
    .WithEnvironment("IdentityAuthority", identity.GetEndpoint("https"));
builder.AddProject<Projects.SampleIdentityConsumer>("usermanagementpoc-sampleidentityconsumer")
    .WithReference(identity)
    .WithEnvironment("IdentityAuthority", identity.GetEndpoint("https"));
builder.Build().Run();