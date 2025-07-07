using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentAI.Domain;

/// <summary>
/// Represents a log of a response or further processing related to a webhook event.
/// </summary>
public class User : BaseEntity
{
    public required string LineUserId { get; set; }
    public required string LineDisplayName { get; set; } = string.Empty;

    [ForeignKey("WebhookEventId")]
    public Guid? WebhookEventId { get; set; }
    public WebhookEvent? WebhookEvent { get; set; }

    public Guid? LatestChatSessionId { get; set; }
    [Required]
    public string? ReplyMode { get; set; } = string.Empty; // ChatSessionReplyModeBinding.AutoReplyByAI or ChatSessionReplyModeBinding.ManualReplyByAdmin
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByLineIdAsync(string LineUserId);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task UpdateLatestChatSessionAsync(Guid userId, Guid chatSessionId, string replyMode);
    Task DeleteAsync(Guid id);
}


