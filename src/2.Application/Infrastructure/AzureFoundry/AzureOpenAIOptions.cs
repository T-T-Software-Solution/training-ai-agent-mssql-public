namespace AgentAI.Application;

public class AzureOpenAIOptions
{
    public required string ChatModel { get; set; }
    public required string TextEmbeddingModel { get; set; }
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
}
