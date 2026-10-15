using System;
using System.Collections.Generic;
using System.Text;
using ChatApp.Domain.Models;

namespace ChatApp.Domain.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(Guid currentUserId, string query);

    // Returns null if the other user doesn't exist, isn't confirmed, or is the caller
    Task<ConversationDto?> GetOrCreateDirectConversationAsync(Guid currentUserId, Guid otherUserId);

    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid currentUserId);

    // The methods below return null/false when the conversation doesn't exist
    // or the caller isn't a participant
    Task<MessageDto?> SendMessageAsync(Guid currentUserId, Guid conversationId, string content);

    Task<IReadOnlyList<MessageDto>?> GetMessagesAsync(Guid currentUserId, Guid conversationId, DateTime? before, int pageSize);

    Task<bool> MarkAsReadAsync(Guid currentUserId, Guid conversationId);
}