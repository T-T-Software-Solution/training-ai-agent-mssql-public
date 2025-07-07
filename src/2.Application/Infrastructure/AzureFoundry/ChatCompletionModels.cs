using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentAI.Application;

public class ChatCompletionModels
{
    public class ChatHistory
    {
        public bool IsBot { get; set; }
        public required string Message { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
