using ChatService.ApiService.Hubs;
using ChatService.DataService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddSignalR();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddCors(opt =>
{
	opt.AddPolicy("reactApp", builder =>
	{
		builder.WithOrigins("http://localhost:3000")
		.AllowAnyHeader().
		AllowAnyMethod().
		AllowCredentials();
	});
});

builder.Services.AddSingleton<SharedDb>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseCors("reactApp");

app.MapHub<ChatHub>("/Chat");

app.MapDefaultEndpoints();

app.Run();