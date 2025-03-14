using ChatService.ApiService.Hubs;
using ChatService.Database;
using ChatService.Database.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ChatDbContext>("ChatDatabase");

builder.Services.AddSignalR();

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
	options.AddPolicy("MyCorsPolicy", policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
		policy.WithOrigins(allowedOrigins)
			  .AllowAnyMethod()
			  .AllowAnyHeader();
	});
});

// Register repositories
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// TODO: Move to servicing
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
	context.Database.Migrate();
}

// TODO: Move to dev environment
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseCors("MyCorsPolicy");

app.MapHub<ChatHub>("/Chat");

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();