using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentAI.Domain
{
    public class WebhookEvent: BaseEntity
    {
        public string? EventJson { get; set; } // JSON string stored as JSONB in DB

        public string? EventType { get; set; }
        public bool Processed { get; set; }
        public DateTimeOffset? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? LineWebhookEventId { get; set; }
        public string? SourceType { get; set; }
        public string? GroupId { get; set; }
        public string? UserId { get; set; }
        public string? ReplyToken { get; set; } // New property for storing reply token
    }

    public interface IWebhookEventRepository
    {
        Task<WebhookEvent?> GetByIdAsync(Guid id);
        Task<IEnumerable<WebhookEvent>> GetAllAsync();
        Task AddAsync(WebhookEvent webhookEvent);
        Task UpdateAsync(WebhookEvent webhookEvent);
        Task DeleteAsync(Guid id);
    }
}
