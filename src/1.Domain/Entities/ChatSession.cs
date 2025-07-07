using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AgentAI.Domain;

/// <summary>
/// Represents a chat message exchanged between user and system, for audit and context.
/// </summary>
public class ChatSession : BaseEntity
{
    [ForeignKey("UserId")]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey("WebhookEventId")]
    public Guid WebhookEventId { get; set; }
    public WebhookEvent? WebhookEvent { get; set; }

    [Required]
    public string ReplyMode { get; set; } = string.Empty; // ChatSessionReplyModeBinding.AutoReplyByAI or ChatSessionReplyModeBinding.ManualReplyByAdmin
}

public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<ChatSession>> GetAllAsync();
    Task<ChatSession> AddAsync(ChatSession chatSession);
    Task UpdateAsync(ChatSession chatSession);
    Task DeleteAsync(Guid id);
}

public class ChatSessionReplyModeBinding
{
    public const string AutoReplyByAI = "Auto Reply By AI";
    public const string ManualReplyByAdmin = "Manual Reply By Admin in Line OA Manager";
}

