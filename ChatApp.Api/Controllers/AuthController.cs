using System.Net;
using System.Security.Claims;
using System.Text;
using ChatApp.Api.Dtos.Auth;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokenService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { message = "Email is already registered." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await TrySendConfirmationEmailAsync(user);

        return Ok(new { message = "Account created. Check your email to confirm your account." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return BadRequest(new { message = "Invalid confirmation link." });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid confirmation link." });
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return BadRequest(new { message = "Invalid or expired confirmation link." });

        return Ok(new { message = "Email confirmed. You can now log in." });
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is not null && !user.EmailConfirmed)
            await TrySendConfirmationEmailAsync(user);

        // Same response either way, so nobody can probe which emails are registered
        return Ok(new { message = "If that account exists and is unconfirmed, a new email has been sent." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.EmailConfirmed)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Please confirm your email before logging in." });

        return Ok(BuildResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Unauthorized();

        return Ok(new { user.Id, user.Email, user.DisplayName });
    }

    private async Task TrySendConfirmationEmailAsync(ApplicationUser user)
    {
        try
        {
            var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            var link = Url.Action(nameof(ConfirmEmail), "Auth",
                           new { userId = user.Id, token = encodedToken }, Request.Scheme)
                       ?? throw new InvalidOperationException("Could not build confirmation link.");

            var html = $"""
                <h2>Welcome to ChatApp, {WebUtility.HtmlEncode(user.DisplayName)}!</h2>
                <p>Please confirm your email address by clicking the link below:</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Confirm my email</a></p>
                <p>If you didn't create this account, you can ignore this email.</p>
                """;

            await _emailService.SendAsync(user.Email!, "Confirm your ChatApp account", html);
        }
        catch (Exception ex)
        {
            // The account exists, so don't fail registration; the user can request a new email
            _logger.LogError(ex, "Failed to send confirmation email to user {UserId}", user.Id);
        }
    }

    private AuthResponse BuildResponse(ApplicationUser user) => new()
    {
        Token = _tokenService.CreateToken(user),
        UserId = user.Id,
        Email = user.Email!,
        DisplayName = user.DisplayName
    };
}