var builder = DistributedApplication.CreateBuilder(args);
 builder.AddProject<Projects.Identity>("usermanagementpoc-identity");
 builder.AddProject<Projects.UserManagementAdmin>("usermanagementpoc-usermanagementadmin");
 builder.AddProject<Projects.SampleIdentityConsumer>("usermanagementpoc-sampleidentityconsumer");
 builder.Build().Run();
