namespace AgentAI.Application;


public interface IChatCompletionInfraService
{
    Task<ChatCompletionModels.ChatCompletionResponse> GetCompletion(string prompt, string? assistantPrompt = null);

    Task<ChatCompletionModels.ChatCompletionResponse> GetSemanticKernelPlugInCompletion(string prompt, string? assistantPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistory = null);
        
    Task<ChatCompletionModels.ChatCompletionResponse> GetSemanticKernelMCPCompletion(string prompt, string? assistantPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistory = null);
}