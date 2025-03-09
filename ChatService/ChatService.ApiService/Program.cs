using ChatService.ApiService.Hubs;
using ChatService.Database;
using ChatService.Database.Repositories;
using ChatService.DataService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ChatDbContext>("ChatDatabase");

builder.Services.AddSignalR();

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddCors(opt =>
{
	opt.AddPolicy("reactApp", builder =>
	{
		builder.WithOrigins("http://localhost:53849")
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<SharedDb>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapOpenApi();

	using (var scope = app.Services.CreateScope())
	{
		var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
		context.Database.EnsureCreated();
	}
}

app.UseCors("reactApp");

app.MapHub<ChatHub>("/Chat");

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();