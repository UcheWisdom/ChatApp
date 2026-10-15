using System;
using System.Collections.Generic;
using System.Text;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Domain.Models;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ChatDbContext _db;

    public ChatService(ChatDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(Guid currentUserId, string query)
    {
        var term = query.Trim();
        if (term.Length < 2)
            return Array.Empty<UserSummaryDto>();

        return await _db.Users
            .Where(u => u.Id != currentUserId
                        && u.EmailConfirmed
                        && (u.UserName!.Contains(term) || u.FirstName.Contains(term) || u.OtherName.Contains(term)))
            .OrderBy(u => u.UserName)
            .Take(20)
            .Select(u => new UserSummaryDto(u.Id, u.UserName!, u.FirstName, u.OtherName))
            .ToListAsync();
    }

    public async Task<ConversationDto?> GetOrCreateDirectConversationAsync(Guid currentUserId, Guid otherUserId)
    {
        if (currentUserId == otherUserId)
            return null;

        var otherExists = await _db.Users.AnyAsync(u => u.Id == otherUserId && u.EmailConfirmed);
        if (!otherExists)
            return null;

        // Reuse the existing 1-on-1 conversation between these two people, if there is one
        var conversationId = await _db.Conversations
            .Where(c => !c.IsGroup
                        && c.Participants.Any(p => p.UserId == currentUserId)
                        && c.Participants.Any(p => p.UserId == otherUserId))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        if (conversationId is null)
        {
            var conversation = new Conversation { IsGroup = false };
            conversation.Participants.Add(new ConversationParticipant { UserId = currentUserId });
            conversation.Participants.Add(new ConversationParticipant { UserId = otherUserId });

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            conversationId = conversation.Id;
        }

        var result = await QueryConversationsAsync(currentUserId, conversationId.Value);
        return result.FirstOrDefault();
    }

    public Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid currentUserId)
        => QueryConversationsAsync(currentUserId, null);

    public async Task<MessageDto?> SendMessageAsync(Guid currentUserId, Guid conversationId, string content)
    {
        var participant = await _db.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (participant is null)
            return null;

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Content = content.Trim()
        };

        _db.Messages.Add(message);

        // Sending a message means the sender has seen everything up to now
        participant.LastReadAt = message.SentAt;

        await _db.SaveChangesAsync();

        var senderUserName = await _db.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.UserName!)
            .FirstAsync();

        return new MessageDto(message.Id, message.ConversationId, message.SenderId,
            senderUserName, message.Content, message.SentAt);
    }

    public async Task<IReadOnlyList<MessageDto>?> GetMessagesAsync(
        Guid currentUserId, Guid conversationId, DateTime? before, int pageSize)
    {
        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (!isParticipant)
            return null;

        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Messages.Where(m => m.ConversationId == conversationId && !m.IsDeleted);

        if (before is not null)
            query = query.Where(m => m.SentAt < before);

        var page = await query
            .OrderByDescending(m => m.SentAt)
            .Take(pageSize)
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderId, m.Sender.UserName!, m.Content, m.SentAt))
            .ToListAsync();

        page.Reverse();   // newest were fetched first; return oldest to newest for display
        return page;
    }

    public async Task<bool> MarkAsReadAsync(Guid currentUserId, Guid conversationId)
    {
        var participant = await _db.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (participant is null)
            return false;

        participant.LastReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // Builds conversation summaries for one user (optionally just one conversation)
    private async Task<IReadOnlyList<ConversationDto>> QueryConversationsAsync(Guid userId, Guid? conversationId)
    {
        var rows = await _db.ConversationParticipants
            .Where(p => p.UserId == userId && (conversationId == null || p.ConversationId == conversationId))
            .Select(p => new
            {
                p.Conversation.Id,
                p.Conversation.IsGroup,
                p.Conversation.Title,
                p.Conversation.CreatedAt,
                Participants = p.Conversation.Participants
                    .Select(x => new UserSummaryDto(x.User.Id, x.User.UserName!, x.User.FirstName, x.User.OtherName))
                    .ToList(),
                LastMessage = p.Conversation.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderId, m.Sender.UserName!, m.Content, m.SentAt))
                    .FirstOrDefault(),
                // Unread = messages from others, sent after this user last read the chat
                UnreadCount = p.Conversation.Messages
                    .Count(m => !m.IsDeleted
                                && m.SenderId != userId
                                && (p.LastReadAt == null || m.SentAt > p.LastReadAt))
            })
            .ToListAsync();

        return rows
            .OrderByDescending(r => r.LastMessage?.SentAt ?? r.CreatedAt)
            .Select(r => new ConversationDto(r.Id, r.IsGroup, r.Title, r.Participants, r.LastMessage, r.UnreadCount))
            .ToList();
    }
}