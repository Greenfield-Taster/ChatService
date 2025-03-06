var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.ChatService_ApiService>("apiservice");

builder.AddProject<Projects.ChatService_Web>("webfrontend")
	.WithExternalHttpEndpoints()
	.WithReference(cache)
	.WaitFor(cache)
	.WithReference(apiService)
	.WaitFor(apiService);

builder.Build().Run();
