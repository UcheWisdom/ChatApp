using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApp.Domain.Models;

public record UserSummaryDto(Guid Id, string UserName, string FirstName, string OtherName);

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderUserName,
    string Content,
    DateTime SentAt);

public record ConversationDto(
    Guid Id,
    bool IsGroup,
    string? Title,
    IReadOnlyList<UserSummaryDto> Participants,
    MessageDto? LastMessage,
    int UnreadCount);