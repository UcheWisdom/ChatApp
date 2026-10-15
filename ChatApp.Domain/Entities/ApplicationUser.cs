using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Identity;

namespace ChatApp.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    //public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string OtherName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {OtherName}".Trim();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }

    public ICollection<ConversationParticipant> Conversations { get; set; } = new List<ConversationParticipant>();
}