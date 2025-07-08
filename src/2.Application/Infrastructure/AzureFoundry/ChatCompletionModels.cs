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

    public class ChatCompletionResponse
    {
        public required string InputPrompt { get; set; } = string.Empty;
        public required string OutputCompletion { get; set; } = string.Empty;
        public int ProcessingTime { get; set; }
        public int InputToken { get; set; }
        public int OutputToken { get; set; }
    }
}
