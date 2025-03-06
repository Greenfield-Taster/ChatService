var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ChatService_ApiService>("apiservice")
	.WithExternalHttpEndpoints();

builder.AddNpmApp("reactFrontend", "../ChatService.Frontend")
	.WithReference(apiService)
	.WaitFor(apiService)
	.WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
	.WithHttpEndpoint(env: "PORT")
	.WithExternalHttpEndpoints()
	.PublishAsDockerFile();

builder.Build().Run();