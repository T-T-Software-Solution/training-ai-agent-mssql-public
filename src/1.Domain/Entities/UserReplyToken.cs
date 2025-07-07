using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentAI.Domain;

/// <summary>
/// Represents a log of a response or further processing related to a webhook event.
/// </summary>
public class UserReplyToken : BaseEntity
{
    [ForeignKey("UserId")]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey("WebhookEventId")]
    public Guid WebhookEventId { get; set; }
    public WebhookEvent? WebhookEvent { get; set; }
    public required string ReplyToken { get; set; } // New property for storing reply token
    public bool IsProcessed { get; set; } // Indicates if the reply token has been processed
    public DateTimeOffset? ProcessedAt { get; set; } // Timestamp when the
}


public interface IUserReplyTokenRepository
{
    Task<UserReplyToken?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserReplyToken>> GetAllAsync();
    Task AddAsync(UserReplyToken userReplyToken);
    Task UpdateReplyTokenAsProcessedAsync(string lineReplyToken, Guid userId);
    Task UpdateReplyTokenAsProcessedAsync(List<string> lineReplyToken, Guid userId);
    Task<List<string>> GetAvailableReplyTokenAsync(Guid userId);
    Task DeleteAsync(Guid id);
}
