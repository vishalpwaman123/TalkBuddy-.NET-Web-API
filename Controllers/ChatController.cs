using Microsoft.AspNetCore.Mvc;
using TalkBuddy.API.Models;
using TalkBuddy.API.Services;

namespace TalkBuddy.API.Controllers;

[ApiController]
[Route("")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Accepts a user message, calls Claude, persists both turns, and returns the assistant reply.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        _logger.LogInformation("POST /chat — user {UserId} session {SessionId}", request.UserId, request.SessionId);
        var reply = await _chatService.ChatAsync(request.UserId, request.SessionId, request.Message);
        return Ok(new ChatResponse(reply));
    }

    /// <summary>
    /// Returns the full conversation history for the given user in chronological order.
    /// </summary>
    [HttpGet("history/{userId}")]
    [ProducesResponseType(typeof(List<Message>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHistory(string userId)
    {
        _logger.LogInformation("GET /history/{UserId}", userId);
        var history = await _chatService.GetHistoryAsync(userId);
        return Ok(history);
    }
}
