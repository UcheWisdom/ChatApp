using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Dtos.Chat;

public class CreateDirectConversationRequest
{
    [Required]
    public Guid OtherUserId { get; set; }
}