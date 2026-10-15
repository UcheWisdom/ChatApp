using ChatApp.Api.Dtos.Chat;
using ChatApp.Api.Extensions;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ConversationsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var conversations = await _chatService.GetConversationsAsync(User.GetUserId());
        return Ok(conversations);
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect(CreateDirectConversationRequest request)
    {
        var conversation = await _chatService.GetOrCreateDirectConversationAsync(
            User.GetUserId(), request.OtherUserId);

        if (conversation is null)
            return BadRequest(new { message = "You can't start a conversation with that user." });

        return Ok(conversation);
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int pageSize = 50)
    {
        var messages = await _chatService.GetMessagesAsync(
            User.GetUserId(), conversationId, before?.UtcDateTime, pageSize);

        if (messages is null)
            return NotFound(new { message = "Conversation not found." });

        return Ok(messages);
    }

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, SendMessageRequest request)
    {
        var message = await _chatService.SendMessageAsync(
            User.GetUserId(), conversationId, request.Content);

        if (message is null)
            return NotFound(new { message = "Conversation not found." });

        return Ok(message);
    }

    [HttpPost("{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId)
    {
        var ok = await _chatService.MarkAsReadAsync(User.GetUserId(), conversationId);

        if (!ok)
            return NotFound(new { message = "Conversation not found." });

        return NoContent();
    }
}