using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;

namespace SemanticKernelPlugins;

public interface ITHChatCompletionService
{
    Task<string> GetCompletion(string prompt, string? assistantPrompt);
    Task<string> GetSemanticKernelPlugInCompletion(string prompt, string? assistantPrompt);
}

public class THChatCompletionService : ITHChatCompletionService
{
    private const string ErrorCallingAI = @"ขออภัยค่ะระบบไม่สามารถประมวลผลคำถามนี้ได้ 
Sorry, I am unable to provide an answer at the moment. Please try again later.";

    private readonly AzureOpenAIOptions _openAIOptions;
    private readonly ILogger<THChatCompletionService> _logger;
    private IChatCompletionService chatCompletionService;
    private IChatCompletionService semanticKernelPlugInService;
    private Kernel semanticKernel;

    public THChatCompletionService(
        IOptions<AzureOpenAIOptions> openAIOptions,
        ILogger<THChatCompletionService> logger)
    {
        _openAIOptions = openAIOptions.Value;
        _logger = logger;

        chatCompletionService = GetChatCompletionService();
        (semanticKernelPlugInService, semanticKernel) = GetSemanticKernelPlugInService();
    }

    // private IChatCompletionService GetChatCompletionService()
    // {
    //     // Create a kernel builder and add the Azure OpenAI chat completion service
    //     var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
    //                             deploymentName: _openAIOptions.ChatModel,
    //                             endpoint: _openAIOptions.Endpoint,
    //                             apiKey: _openAIOptions.ApiKey);

    //     // Add Serilog logging
    //     kernelBuilder.Services.AddSerilog();

    //     // Build the kernel
    //     Kernel kernel = kernelBuilder.Build();

    //     // Get the chat completion service from the kernel
    //     return kernel.GetRequiredService<IChatCompletionService>();
    // }
    // private (IChatCompletionService, Kernel) GetSemanticKernelPlugInService()
    // {
    //     // Create a kernel builder and add the Azure OpenAI chat completion service
    //     var kernelBuilder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
    //                             deploymentName: _openAIOptions.ChatModel,
    //                             endpoint: _openAIOptions.Endpoint,
    //                             apiKey: _openAIOptions.ApiKey);

    //     // Add Serilog logging
    //     kernelBuilder.Services.AddSerilog();

    //     // Build the kernel
    //     var semanticKernelLocal = kernelBuilder.Build();

    //     // Add the lights plugin to the kernel
    //     semanticKernelLocal.Plugins.AddFromType<LightsPlugin>("Lights");
    // // semanticKernelLocal.Plugins.AddFromType<WeatherForecastPlugin>("WeatherForecast");

    //     // Get the chat completion service from the kernel
    //     var semanticKernelPlugInServiceLocal = semanticKernelLocal.GetRequiredService<IChatCompletionService>();

    //     return (semanticKernelPlugInServiceLocal, semanticKernelLocal);
    // }
    // public async Task<string> GetCompletion(string prompt, string? systemPrompt)
    // {
    //     _logger.LogInformation("Prompt: {Prompt}", prompt);

    //     // Create a chat history object
    //     ChatHistory chatHistory = new ChatHistory();

    //     var isContainsSystemPrompt = !string.IsNullOrEmpty(systemPrompt);
    //     if (isContainsSystemPrompt)
    //     {
    //         // Add the assistant prompt to the chat history
    //         chatHistory.AddSystemMessage(systemPrompt!);
    //     }

    //     // Add the user prompt to the chat history
    //     chatHistory.AddUserMessage(prompt);

    //     try
    //     {
    //         // Get the response from the AI
    //         var startupTime = DateTime.UtcNow;
    //         var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory: chatHistory);
    //         var endTime = DateTime.UtcNow;
    //         var processingTime = (int)(endTime - startupTime).TotalSeconds;

    //         int? inputToken = null;
    //         int? outputToken = null;
    //         if (result.InnerContent is OpenAI.Chat.ChatCompletion chatCompletion)
    //         {
    //             inputToken = chatCompletion.Usage?.InputTokenCount;
    //             outputToken = chatCompletion.Usage?.OutputTokenCount;
    //         }

    //         // Log the role and content of the response
    //         _logger.LogInformation("Role: {Role}, Content: {Content}", result.Role, result.Content);
    //         _logger.LogInformation("Input Token: {InputToken}, Output Token: {OutputToken}", inputToken, outputToken);
    //         _logger.LogInformation("Processing Time: {ProcessingTime} seconds", processingTime);
    //         _logger.LogInformation("Total Token: {TotalToken}", (inputToken ?? 0) + (outputToken ?? 0));

    //         // Log the cost of the request
    //         var costInputToken = inputToken * 0.15 / 1000000;
    //         var costOutputToken = outputToken * 0.6 / 1000000;

    //         _logger.LogInformation($"Total Cost (USD): {costInputToken + costOutputToken}");
    //         _logger.LogInformation($"Total Cost (THB): {(costInputToken + costOutputToken) * 35}");


    //         // Add the message from the agent to the chat history
    //         chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);

    //         return result.Content ?? string.Empty;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting completion from AI");

    //         return ErrorCallingAI;
    //     }
    // }

    // public async Task<string> GetSemanticKernelPlugInCompletion(string prompt, string? systemPrompt)
    // {
    //     _logger.LogInformation("Prompt: {Prompt}", prompt);

    //     // Create a chat history object
    //     ChatHistory chatHistory = new ChatHistory();

    //     var isContainsSystemPrompt = !string.IsNullOrEmpty(systemPrompt);
    //     if (isContainsSystemPrompt)
    //     {
    //         // Add the assistant prompt to the chat history
    //         chatHistory.AddSystemMessage(systemPrompt!);
    //     }

    //     // Add the user prompt to the chat history
    //     chatHistory.AddUserMessage(prompt);

    //     try
    //     {
    //         // Get the response from the AI
    //         var startupTime = DateTime.UtcNow;
    //         var result = await semanticKernelPlugInService.GetChatMessageContentAsync(
    //             chatHistory: chatHistory,
    //             executionSettings: new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() },
    //             kernel: semanticKernel
    //             );
    //         var endTime = DateTime.UtcNow;
    //         var processingTime = (int)(endTime - startupTime).TotalSeconds;

    //         int? inputToken = null;
    //         int? outputToken = null;
    //         if (result.InnerContent is OpenAI.Chat.ChatCompletion chatCompletion)
    //         {
    //             inputToken = chatCompletion.Usage?.InputTokenCount;
    //             outputToken = chatCompletion.Usage?.OutputTokenCount;
    //         }

    //         // Log the role and content of the response
    //         _logger.LogInformation("Role: {Role}, Content: {Content}", result.Role, result.Content);
    //         _logger.LogInformation("Input Token: {InputToken}, Output Token: {OutputToken}", inputToken, outputToken);
    //         _logger.LogInformation("Processing Time: {ProcessingTime} seconds", processingTime);
    //         _logger.LogInformation("Total Token: {TotalToken}", (inputToken ?? 0) + (outputToken ?? 0));

    //         // Log the cost of the request
    //         var costInputToken = inputToken * 0.15 / 1000000;
    //         var costOutputToken = outputToken * 0.6 / 1000000;

    //         _logger.LogInformation($"Total Cost (USD): {costInputToken + costOutputToken}");
    //         _logger.LogInformation($"Total Cost (THB): {(costInputToken + costOutputToken) * 35}");


    //         // Add the message from the agent to the chat history
    //         chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);

    //         return result.Content ?? string.Empty;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting completion from AI");

    //         return ErrorCallingAI;
    //     }
    // }

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

    // private class WeatherForecastPlugin
    // {
    //     private readonly HttpClient _httpClient = new();
    //     private const string ApiUrlTemplate = "https://wttr.in/{0}?format=j1";

    //     [KernelFunction("get_weather_forecast")]
    //     [Description("Gets the 3-day weather forecast for a specified city using the global free wttr.in API.")]
    //     [return: Description("A string containing the weather forecast summary for the city.")]
    //     public async Task<string> GetWeatherForecastAsync([Description("City name in English")] string city)
    //     {
    //         try
    //         {
    //             var url = string.Format(ApiUrlTemplate, Uri.EscapeDataString(city));
    //             var response = await _httpClient.GetAsync(url);
    //             response.EnsureSuccessStatusCode();
    //             var json = await response.Content.ReadAsStringAsync();
    //             // Optionally, parse and summarize the JSON here
    //             return json;
    //         }
    //         catch (Exception ex)
    //         {
    //             return $"Error fetching weather forecast: {ex.Message}";
    //         }
    //     }
    // }

    private class LightModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("thainame")]
        public string? ThaiName { get; set; }

        [JsonPropertyName("is_on")]
        public bool? IsOn { get; set; }
    }
}


