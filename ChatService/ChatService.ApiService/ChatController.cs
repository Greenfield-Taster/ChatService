using ChatService.Database.Models;
using ChatService.Database.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.ApiService;

[Route("api/[controller]")]
[ApiController]
public class ChatController(IChatRepository chatRepository) : ControllerBase
{
	[HttpGet("messages")]
	public IActionResult GetMessages()
	{
		// Simulate fetching messages from a database
		var messages = new List<string>
		{
			"Hello, how are you?",
			"I'm fine, thank you!",
			"What about you?"
		};
		return Ok(messages);
	}

	[HttpPost("send")]
	public async Task<IActionResult> SendMessage([FromBody] string message)
	{
		await chatRepository.StoreMessageAsync(new ChatMessage
		{
			Id = Guid.NewGuid(),
			SenderId = Guid.NewGuid(), // This should be the actual sender's ID
			RoomId = Guid.NewGuid(), // This should be the actual room's ID
			Content = message
		});

		return Ok($"Message sent: {message}");
	}
}