using ChatApp.Api.Extensions;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IChatService _chatService;

    public UsersController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var users = await _chatService.SearchUsersAsync(User.GetUserId(), query ?? string.Empty);
        return Ok(users);
    }
}