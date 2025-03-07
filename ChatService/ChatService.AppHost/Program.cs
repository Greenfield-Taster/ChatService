var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddPostgres("postgresServer")
	.WithPgAdmin();

var chatDatabase = postgresServer.AddDatabase("ChatDatabase");

var apiService = builder.AddProject<Projects.ChatService_ApiService>("apiservice")
	.WithExternalHttpEndpoints()
	.WithReference(chatDatabase)
	.WaitFor(chatDatabase);

builder.AddNpmApp("ChatFrontend", "../chat-app")
	.WithReference(apiService)
	.WaitFor(apiService)
	.WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
	.WithHttpEndpoint(port: 53849, env: "PORT")
	.WithExternalHttpEndpoints()
	.PublishAsDockerFile();

builder.Build().Run();