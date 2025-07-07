using Microsoft.Extensions.Logging;
using AgentAI.Application;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json.Serialization;
using System.ComponentModel;
using ModelContextProtocol.Client;
using Microsoft.Identity.Client;

namespace AgentAI.Infrastructure;

/// <summary>
/// Implementation of the embedding service using OpenAI models.
/// </summary>
public class ChatCompletionInfraService : IChatCompletionInfraService
{
    private const string ErrorCallingAI = @"ขออภัยค่ะระบบไม่สามารถประมวลผลคำถามนี้ได้ 
Sorry, I am unable to provide an answer at the moment. Please try again later.";

    private readonly AzureOpenAIOptions _openAIOptions;
    private readonly ILogger<ChatCompletionInfraService> _logger;
    private readonly AzureAdOptions _azureAdOptions;
    private readonly MCPOptions _mcpOptions;

    public ChatCompletionInfraService(
        IOptions<AzureOpenAIOptions> openAIOptions,
        ILogger<ChatCompletionInfraService> logger,
        IOptions<AzureAdOptions> azureAdOptions,
        IOptions<MCPOptions> mcpOptions)
    {
        _openAIOptions = openAIOptions.Value;
        _logger = logger;
        _azureAdOptions = azureAdOptions.Value;
        _mcpOptions = mcpOptions.Value;
    }

    public async Task<string> GetCompletion(string prompt, string? assistantPrompt = null)
    {
        _logger.LogInformation("Prompt: {Prompt}", prompt);

        var chatCompletionService = GetChatCompletionService();

        var chatHistory = PrepareGetCompletionChatHistories(prompt, assistantPrompt);

        try
        {
            // Get the response from the AI
            var startupTime = DateTime.UtcNow;

            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory: chatHistory);

            LogLLMUsages(startupTime, result);

            return result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from AI");

            return ErrorCallingAI;
        }
    }

    private IChatCompletionService GetChatCompletionService()
    {
        // Create a kernel builder and add the Azure OpenAI chat completion service
        var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                                deploymentName: _openAIOptions.ChatModel,
                                endpoint: _openAIOptions.Endpoint,
                                apiKey: _openAIOptions.ApiKey);

        // Add Serilog logging
        kernelBuilder.Services.AddSerilog();

        // Build the kernel
        Kernel kernel = kernelBuilder.Build();

        // Get the chat completion service from the kernel
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        return chatCompletionService;
    }

    private static ChatHistory PrepareGetCompletionChatHistories(string prompt, string? assistantPrompt)
    {
        // Create a chat history object
        ChatHistory chatHistory = new ChatHistory();

        if (assistantPrompt != null)
        {
            // Add the assistant prompt to the chat history
            chatHistory.AddSystemMessage(assistantPrompt);
        }

        // Add the user prompt to the chat history
        chatHistory.AddUserMessage(prompt);
        return chatHistory;
    }

    private void LogLLMUsages(DateTime startupTime, ChatMessageContent result)
    {
        var endTime = DateTime.UtcNow;
        var processingTime = (int)(endTime - startupTime).TotalSeconds;

        int? inputToken = null;
        int? outputToken = null;
        if (result.InnerContent is OpenAI.Chat.ChatCompletion chatCompletion)
        {
            inputToken = chatCompletion.Usage?.InputTokenCount;
            outputToken = chatCompletion.Usage?.OutputTokenCount;
        }

        // Log the role and content of the response
        _logger.LogInformation("Role: {Role}, Content: {Content}", result.Role, result.Content);
        _logger.LogInformation("Input Token: {InputToken}, Output Token: {OutputToken}", inputToken, outputToken);
        _logger.LogInformation("Processing Time: {ProcessingTime} seconds", processingTime);
        _logger.LogInformation("Total Token: {TotalToken}", (inputToken ?? 0) + (outputToken ?? 0));

        // Log the cost of the request
        var costInputToken = inputToken * 0.15 / 1000000;
        var costOutputToken = outputToken * 0.6 / 1000000;

        _logger.LogInformation($"Total Cost (USD): {costInputToken + costOutputToken}");
        _logger.LogInformation($"Total Cost (THB): {(costInputToken + costOutputToken) * 35}");
    }

    public async Task<string> GetSemanticKernelPlugInCompletion(string prompt, string? systemPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistoryFromSession = null)
    {
        _logger.LogInformation("Prompt: {Prompt}", prompt);

        var (semanticKernel, semanticKernelPlugInService) = GetSemanticKernelPlugInService();

        var chatHistory = PrepareGetSemanticKernelPlugInCompletionChatHistories(prompt, systemPrompt, chatHistoryFromSession);

        try
        {
            // Get the response from the AI
            var startupTime = DateTime.UtcNow;

            var result = await semanticKernelPlugInService.GetChatMessageContentAsync(
                chatHistory: chatHistory,
                executionSettings: new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
                kernel: semanticKernel
                );

            LogLLMUsages(startupTime, result);

            return result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting completion from AI");

            return ErrorCallingAI;
        }
    }

    private (Kernel, IChatCompletionService) GetSemanticKernelPlugInService()
    {
        // Create a kernel builder and add the Azure OpenAI chat completion service
        var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                                deploymentName: _openAIOptions.ChatModel,
                                endpoint: _openAIOptions.Endpoint,
                                apiKey: _openAIOptions.ApiKey);

        // Add Serilog logging
        kernelBuilder.Services.AddSerilog();

        // Build the kernel
        var semanticKernel = kernelBuilder.Build();

        // Add the lights plugin to the kernel
        semanticKernel.Plugins.AddFromType<LightsPlugin>("Lights");
        semanticKernel.Plugins.AddFromType<WeatherForecastPlugin>("WeatherForecast");

        // Get the chat completion service from the kernel
        var semanticKernelPlugInService = semanticKernel.GetRequiredService<IChatCompletionService>();

        return (semanticKernel, semanticKernelPlugInService);
    }

    private static ChatHistory PrepareGetSemanticKernelPlugInCompletionChatHistories(string prompt, string? systemPrompt, IEnumerable<ChatCompletionModels.ChatHistory>? chatHistoryFromSession)
    {
        ChatHistory chatHistory = new ChatHistory();

        if (systemPrompt != null)
        {
            // Add the assistant prompt to the chat history
            chatHistory.AddSystemMessage(systemPrompt);
        }

        // If chat history from session is provided, add it to the chat history
        if (chatHistoryFromSession != null && chatHistoryFromSession.Any())
        {
            foreach (var chat in chatHistoryFromSession.OrderBy(c => c.CreatedAt))
            {
                if (chat.IsBot)
                {
                    chatHistory.AddAssistantMessage(chat.Message);
                }
                else
                {
                    chatHistory.AddUserMessage(chat.Message);
                }
            }
        }

        // Add the user prompt to the chat history
        chatHistory.AddUserMessage(prompt);
        return chatHistory;
    }

    public async Task<string> GetSemanticKernelMCPCompletion(string prompt, string? systemPrompt = null,
        IEnumerable<ChatCompletionModels.ChatHistory>? chatHistoryFromSession = null)
    {
        _logger.LogInformation("Prompt: {Prompt}", prompt);

        AuthenticationResult? adToken = await GetAzureAdToken();

        // Wrap the MCP client in an 'await using' block to ensure it's disposed correctly
        await using (IMcpClient mcpClient = await GetMcpClient(adToken))
        {
            IList<McpClientTool> tools = await GetMcpTools(mcpClient);

            var (semanticKernel, semanticKernelMcpService) = GetSemanticKernelMcpService(tools);

            var chatHistory = PrepareGetSemanticKernelPlugInCompletionChatHistories(prompt, systemPrompt, chatHistoryFromSession);

            try
            {
                // Get the response from the AI
                var startupTime = DateTime.UtcNow;

                var result = await semanticKernelMcpService.GetChatMessageContentAsync(
                    chatHistory: chatHistory,
                    executionSettings: new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
                    kernel: semanticKernel
                    );
                
                LogLLMUsages(startupTime, result);

                return result.Content ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion from AI");

                return ErrorCallingAI;
            }
        }
    }

    private (Kernel, IChatCompletionService) GetSemanticKernelMcpService(IList<McpClientTool> tools)
    {
        // Create a kernel builder and add the Azure OpenAI chat completion service
        var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                                deploymentName: _openAIOptions.ChatModel,
                                endpoint: _openAIOptions.Endpoint,
                                apiKey: _openAIOptions.ApiKey);

        // Add Serilog logging
        kernelBuilder.Services.AddSerilog();

        // Build the kernel
        var semanticKernel = kernelBuilder.Build();

        #pragma warning disable SKEXP0001 // AsKernelFunction is for evaluation purposes only
        semanticKernel.Plugins.AddFromFunctions("DealerTools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
        #pragma warning restore SKEXP0001

        // Get the chat completion service from the kernel
        var semanticKernelPlugInService = semanticKernel.GetRequiredService<IChatCompletionService>();
        // Add the MCP client to the semantic kernel

        return (semanticKernel, semanticKernelPlugInService);
    }

    private async Task<IMcpClient> GetMcpClient(AuthenticationResult? adToken)
    {
        if (adToken == null)
        {
            throw new InvalidOperationException("Azure AD token is null. Cannot create MCP client.");
        }

        return await McpClientFactory.CreateAsync(
                    new SseClientTransport(new()
                    {
                        Endpoint = new Uri(_mcpOptions.Endpoint),
                        AdditionalHeaders = new Dictionary<string, string>
                        {
                            { "Authorization", $"Bearer {adToken.AccessToken}" }
                        }
                    }));
    }

    private async Task<IList<McpClientTool>> GetMcpTools(IMcpClient mcpClient)
    {
        // List all available tools from the MCP server.
        Console.WriteLine("Available tools:");

        IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
        foreach (McpClientTool tool in tools)
        {
            Console.WriteLine($"{tool.Name} - {tool.Description}");
        }

        Console.WriteLine();

        return tools;
    }

    private async Task<AuthenticationResult?> GetAzureAdToken()
    {
        var authority = $"https://login.microsoftonline.com/{_azureAdOptions.TenantId}";

        var app = ConfidentialClientApplicationBuilder.Create(_azureAdOptions.ClientId)
            .WithClientSecret(_azureAdOptions.ClientSecret)
            .WithAuthority(authority)
            .Build();

        string[] scopes = new[] { _azureAdOptions.Audience ?? string.Empty };

        AuthenticationResult? resultX = null;
        try
        {
            resultX = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            Console.WriteLine("==== Azure Access Token: =====");
            Console.WriteLine(resultX.AccessToken);
        }
        catch (MsalClientException ex)
        {
            Console.WriteLine($"Error acquiring token: {ex.Message}");
        }
        if (resultX == null)
        {
            Console.WriteLine("Failed to acquire access token. Exiting.");
        }

        return resultX;
    }

    private class LightsPlugin
    {
        // Mock data for the lights
        private readonly List<LightModel> lights = new()
            {
                new LightModel { Id = 1, Name = "Table Lamp", ThaiName = "โคมไฟตั้งโต๊ะ", IsOn = false },
                new LightModel { Id = 2, Name = "Porch light", ThaiName = "ไฟระเบียง", IsOn = false },
                new LightModel { Id = 3, Name = "Chandelier", ThaiName = "โคมไฟระย้า", IsOn = false }
            };

        [KernelFunction("get_lights")]
        [Description("Gets a list of lights and their current state")]
        [return: Description("An array of lights")]
        public Task<List<LightModel>> GetLightsAsync()
        {
            return Task.FromResult(lights);
        }

        [KernelFunction("change_state")]
        [Description("Changes the state of the light")]
        [return: Description("The updated state of the light; will return null if the light does not exist")]
        public Task<LightModel?> ChangeStateAsync(int id, bool isOn)
        {
            var light = lights.FirstOrDefault(light => light.Id == id);

            if (light == null)
            {
                return Task.FromResult<LightModel?>(null);
            }

            // Update the light with the new state
            light.IsOn = isOn;

            return Task.FromResult<LightModel?>(light);
        }
    }

    private sealed class WeatherForecastPlugin
    {
        private readonly HttpClient _httpClient = new();
        private const string ApiUrlTemplate = "https://wttr.in/{0}?format=j1";

        [KernelFunction("get_weather_forecast")]
        [Description("Gets the 3-day weather forecast for a specified city using the global free wttr.in API.")]
        [return: Description("A string containing the weather forecast summary for the city.")]
        public async Task<string> GetWeatherForecastAsync([Description("City name in English")] string city)
        {
            try
            {
                var url = string.Format(ApiUrlTemplate, Uri.EscapeDataString(city));
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                // Optionally, parse and summarize the JSON here
                return json;
            }
            catch (Exception ex)
            {
                return $"Error fetching weather forecast: {ex.Message}";
            }
        }
    }

    private sealed class LightModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("thainame")]
        public required string ThaiName { get; set; }

        [JsonPropertyName("is_on")]
        public bool? IsOn { get; set; }
    }
}
