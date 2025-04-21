using ChatService.ApiService.Hubs;
using ChatService.Database;
using ChatService.Database.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
 
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ChatDbContext>("ChatDatabase");

builder.Services.AddSignalR();

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
 
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://greenfield-taster.github.io", "https://wonderful-island-0956e1103.6.azurestaticapps.net") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});
 
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddOpenApi();

var app = builder.Build();
 
app.UseExceptionHandler();
 
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
	context.Database.Migrate();
}

//app.UseCors("MyCorsPolicy");
app.UseCors("AllowSpecificOrigins");

app.MapHub<ChatHub>("/chatHub");

app.MapControllers();

app.MapDefaultEndpoints();

app.MapGet("/migrateDatabase", async () =>
{
	// TODO: Move to servicing
	using var scope = app.Services.CreateScope();
	var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
	await context.Database.MigrateAsync();

	return Results.Ok("Database migrated");
});

app.Run();