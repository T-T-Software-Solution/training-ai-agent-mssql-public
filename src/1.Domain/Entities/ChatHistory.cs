using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AgentAI.Domain;

/// <summary>
/// Represents a chat message exchanged between user and system, for audit and context.
/// </summary>
public class ChatHistory : BaseEntity
{
    [ForeignKey("ChatSessionId")]
    public Guid ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public Guid WebhookEventId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string SenderType { get; set; } = string.Empty; // User or Bot from ChatHistorySendTextBinding

    [Required]
    public string MessageMode { get; set; } = string.Empty; // Auto or Manual from ChatHistoryMessageModeBinding

    public string? LLMsInput { get; set; }
    public int? LLMsProcessingTime { get; set; } // Seconds
    public int? LLMsInputToken { get; set; } // Tokens used in the request
    public int? LLMsOutputToken { get; set; } // Tokens used in the response
    public int? LLMsPercentAccuracyByHuman { get; set; }
    public int? LLMsPercentAccuracyByAI { get; set; }
}

public interface IChatHistoryRepository
    {
        Task<ChatHistory?> GetByIdAsync(Guid id);
        Task AddAsync(ChatHistory chatHistory);
        Task DeleteAsync(Guid id);
        Task<List<ChatHistory>> GetBySessionIdAsync(Guid sessionId);
    }

public class ChatHistorySendTextBinding
{
    public const string User = "User";
    public const string Bot = "Bot";
}

public class ChatHistorySendTextEnum
{
    public enum Mode
    {
        User,
        Bot
    }
}

public class ChatHistoryMessageModeBinding
{
    public const string Auto = "Auto";
    public const string Manual = "Manual";
}