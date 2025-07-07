namespace AgentAI.Application;


public interface IChatCompletionInfraService
{
    Task<string> GetCompletion(string prompt, string? assistantPrompt = null);

    Task<string> GetSemanticKernelPlugInCompletion(string prompt, string? assistantPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistory = null);
        
    Task<string> GetSemanticKernelMCPCompletion(string prompt, string? assistantPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistory = null);
}