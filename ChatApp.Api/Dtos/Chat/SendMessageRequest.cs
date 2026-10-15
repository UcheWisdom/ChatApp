using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Dtos.Chat;

public class SendMessageRequest
{
    [Required, StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}