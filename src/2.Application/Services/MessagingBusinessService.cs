using System.Text.Json;
using Microsoft.Extensions.Logging;
using AgentAI.Domain;
using System.Threading.Tasks;

namespace AgentAI.Application;

/// <summary>
/// Interface for messaging business service.
/// </summary>
public interface IMessagingBusinessService
{
    Task HandleWebhookAsync(string body, string? signature);
    Task<string> GenerateSignatureAsync(string body);
    Task<bool> ReplyToLineUserAsync(string accessToken, string messageText, string replyToken);
    Task RecordChatHistory(Guid chatSessionId, Guid webhookEventId, string? messageText, ChatHistorySendTextEnum.Mode senterType);
}

/// <summary>
/// Implementation of the messaging business service.
/// </summary>
public class MessagingBusinessService : IMessagingBusinessService
{
    private readonly ILineMessagingInfraService _lineInfraService;
    private readonly ILogger<MessagingBusinessService> _logger;
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IUserBusinessService _userBusinessService;
    private readonly IChatHistoryRepository _chatHistoryRepository;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IChatCompletionInfraService _chatCompletionInfraService;

    public MessagingBusinessService(
        ILineMessagingInfraService lineInfraService,
        ILogger<MessagingBusinessService> logger,
        IWebhookEventRepository webhookEventRepository,
        IUserBusinessService userBusinessService,
        IChatHistoryRepository chatHistoryRepository,
        IChatSessionRepository chatSessionRepository,
        IChatCompletionInfraService chatCompletionInfraService)
    {
        _lineInfraService = lineInfraService;
        _logger = logger;
        _webhookEventRepository = webhookEventRepository;
        _userBusinessService = userBusinessService;
        _chatHistoryRepository = chatHistoryRepository;
        _chatSessionRepository = chatSessionRepository;
        _chatCompletionInfraService = chatCompletionInfraService;
    }

    /// <inheritdoc/>
    public async Task HandleWebhookAsync(string body, string? signature)
    {
        ValidateSignatureOrThrow(body, signature);
        var webhookRequest = DeserializeWebhookRequestOrThrow(body);
        if (webhookRequest.Events == null)
            return;

        var accessToken = await GetAccessTokenOrThrow();
        foreach (var lineEvent in webhookRequest.Events)
        {
            await ProcessLineEventAsync(lineEvent, accessToken);
        }
    }

    public Task<string> GenerateSignatureAsync(string body)
    {
        var signature = _lineInfraService.GenerateSignature(body);
        return Task.FromResult(signature);
    }

    public async Task<bool> ReplyToLineUserAsync(string accessToken, string messageText, string replyToken)
    {
        return await _lineInfraService.SendMessageAsync(accessToken, messageText, replyToken);
    }

    public async Task RecordChatHistory(Guid chatSessionId, Guid webhookEventId, string? messageText, ChatHistorySendTextEnum.Mode senterType)
    {
        var senderType = senterType switch
        {
            ChatHistorySendTextEnum.Mode.User => ChatHistorySendTextBinding.User,
            ChatHistorySendTextEnum.Mode.Bot => ChatHistorySendTextBinding.Bot,
            _ => throw new ArgumentException("Invalid sender type")
        };

        await _chatHistoryRepository.AddAsync(new ChatHistory
        {
            Id = Guid.NewGuid(),
            ChatSessionId = chatSessionId,
            WebhookEventId = webhookEventId,
            Message = messageText ?? string.Empty,
            SenderType = senderType
        });
    }

    private void ValidateSignatureOrThrow(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature) || !_lineInfraService.VerifySignature(body, signature))
        {
            _logger.LogWarning("Invalid LINE signature received.");
            throw new UnauthorizedAccessException("Invalid LINE signature.");
        }
    }

    private LineMessagingAPI.WebhookRequest DeserializeWebhookRequestOrThrow(string body)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
        var webhookRequest = JsonSerializer.Deserialize<LineMessagingAPI.WebhookRequest>(body, options);
        if (webhookRequest == null)
        {
            _logger.LogError("Invalid JSON format received in webhook body.");
            throw new ArgumentException("Invalid JSON format.");
        }
        return webhookRequest;
    }

    private async Task<string> GetAccessTokenOrThrow()
    {
        var accessToken = await _lineInfraService.LineLoginAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError("Failed to obtain access token from LINE API.");
            throw new InvalidOperationException("Failed to obtain access token.");
        }
        return accessToken;
    }

    private async Task ProcessLineEventAsync(LineMessagingAPI.WebhookRequest.Event lineEvent, string accessToken)
    {
        if (string.IsNullOrEmpty(lineEvent.Source?.UserId))
        {
            _logger.LogWarning("Received LINE event without UserId: {Event}", JsonSerializer.Serialize(lineEvent));
            throw new ArgumentException("Event source UserId is required.");
        }

        // Record the webhook event
        var webhookEvent = await RecordWebhookFromLineEvent(lineEvent);

        // Extract relevant information from the event using a helper class
        var info = new LineEventInfo(lineEvent);

        // If the event contains a message, reply token, and user ID, send a response
        // Otherwise, log the event without processing
        if (string.IsNullOrEmpty(info.MessageText) || string.IsNullOrEmpty(info.ReplyToken) || string.IsNullOrEmpty(info.UserId))
        {
            _logger.LogInformation("Received event without message or reply token: {Event}", JsonSerializer.Serialize(lineEvent));
            return;
        }

        try
        {
            // Log the user loading process
            await _lineInfraService.LineLoading(accessToken, info.UserId);

            // Register or update the user in the system
            User existingUser = await RegisterLineUserToTheSystem(lineEvent, accessToken, webhookEvent);

            // Save the reply token for the user
            await _userBusinessService.SaveLineReplyTokenAsync(info.ReplyToken, existingUser.Id, webhookEvent.Id);

            // Get the user's chat mode based on the message text
            var chatMode = GetChatMode(info.MessageText);

            // Get the user's chat Mode
            var chatSessionId = await SetUserChatMode(chatMode, webhookEvent, existingUser, info.MessageText);

            // Record the chat history for the user 
            await RecordUserChatHistory(webhookEvent, info, chatSessionId, chatMode);

            var botReplyMessage = GetGreetingMessageFromChatSelectionMode(info.MessageText);
            if (IsGreetingMessage(botReplyMessage))
            {
                await HandleAIAutoReplyAsync(webhookEvent, chatSessionId, botReplyMessage, accessToken, info.ReplyToken);
                return;
            }

            if (IsAIAutoReply(existingUser))
            {
                var llmResponse = await PerformCallingLLM(info.MessageText, chatSessionId);

                await HandleAILLMsReplyAsync(webhookEvent, chatSessionId, accessToken, info.ReplyToken, llmResponse);

                return;
            }

        }
        catch (Exception ex)
        {
            await HandleLineEventErrorAsync(ex, webhookEvent);

            throw;
        }
    }

    private async Task<ChatCompletionModels.ChatCompletionResponse> PerformCallingLLM(string message, Guid sessionId)
    {
        var assistantPrompt = @"
You are “DealerDMS Pro,” a domain-expert assistant for the Powersports industry.
Your mission is to help dealership owners, managers, and staff get practical, up-to-date guidance on every aspect of a Dealership Management System (DMS): inventory, sales, F&I, service & parts, CRM, reporting, and integrations (OEM feeds, accounting, e-commerce, marketing, etc.).

Guidelines
1. Depth & Clarity • Give step-by-step explanations, best practices, and real-world examples drawn from Powersports dealerships (motorcycles, ATVs, UTVs, PWC, snowmobiles).  
2. Relevance • Tailor answers to the user’s specific dealership size, brands carried, and pain points; ask concise follow-up questions when details are missing.  
3. Actionability • Provide checklists, configuration tips, KPIs, and implementation timelines that users can apply immediately.  
4. Neutrality • Discuss leading DMS vendors (e.g., CDK Lightspeed, DX1, Dealertrack, Blackpurl) objectively—compare features, pricing tiers, pros/cons—without endorsing one unless the user requests a recommendation.  
5. Compliance & Security • Flag any data-privacy, PCI-DSS, or manufacturer-compliance considerations relevant to the suggested actions.  
6. Tone • Professional, friendly, jargon-aware but plain-English; keep answers concise unless deeper detail is requested.  
7. Format • Use bullet points or numbered steps for clarity; include code-style blocks only for snippets (SQL, API calls, report formulas).  
8. Limitations • You are not a lawyer or accountant; for legal, tax, or HR matters, advise consulting a certified professional.

Always end with: “Let me know if you’d like more detail or examples for your dealership.”";

        var chatHistory = await GetChatHistoryAsync(sessionId);

        var result = await _chatCompletionInfraService.GetSemanticKernelPlugInCompletion(
                        prompt: message,
                        assistantPrompt: assistantPrompt,
                        chatHistory: chatHistory);

        // chatHistory.Add(new ChatCompletionModels.ChatHistory
        // {
        //     IsBot = false,
        //     Message = "[userid:111][usergroup:222]",
        //     CreatedAt = DateTimeOffset.UtcNow
        // });
        // var result = await _chatCompletionInfraService.GetSemanticKernelMCPCompletion(
        //                 prompt: message,
        //                 assistantPrompt: assistantPrompt,
        //                 chatHistory: chatHistory);

        return result;
    }

    private async Task<List<ChatCompletionModels.ChatHistory>> GetChatHistoryAsync(Guid sessionId)
    {
        // Fetch chat history for the given session ID
        var chatHistory = await _chatHistoryRepository.GetBySessionIdAsync(sessionId);
        if (chatHistory == null || !chatHistory.Any())
        {
            _logger.LogWarning("No chat history found for session ID: {SessionId}", sessionId);
            return new List<ChatCompletionModels.ChatHistory>();
        }

        return chatHistory.Select(ch => new ChatCompletionModels.ChatHistory
        {
            IsBot = ch.SenderType == ChatHistorySendTextBinding.Bot,
            Message = ch.Message,
            CreatedAt = ch.CreatedAt
        }).ToList();
    }

    private async Task RecordUserChatHistory(WebhookEvent webhookEvent, LineEventInfo info, Guid chatSessionId, ChatModeConstants.Enums chatMode)
    {
        _logger.LogInformation("User message: {UserMessage}", info.MessageText);

        var messageMode = chatMode switch
        {
            ChatModeConstants.Enums.SelectAgent => ChatHistoryMessageModeBinding.Auto,
            ChatModeConstants.Enums.SelectHumanAdmin => ChatHistoryMessageModeBinding.Auto,
            _ => ChatHistoryMessageModeBinding.Manual // Default to Auto for other modes
        };

        // Record the chat history for the user
        await _chatHistoryRepository.AddAsync(new ChatHistory
        {
            Id = Guid.NewGuid(),
            ChatSessionId = chatSessionId,
            WebhookEventId = webhookEvent.Id,
            Message = info.MessageText ?? string.Empty,
            SenderType = ChatHistorySendTextBinding.User,
            MessageMode = messageMode
        });
    }

    private async Task RecordBotChatHistory(WebhookEvent webhookEvent, Guid chatSessionId, string botReplyMessage)
    {
        _logger.LogInformation("Bot reply message: {BotReplyMessage}", botReplyMessage);

        await _chatHistoryRepository.AddAsync(new ChatHistory
        {
            Id = Guid.NewGuid(),
            ChatSessionId = chatSessionId,
            WebhookEventId = webhookEvent.Id,
            Message = botReplyMessage,
            SenderType = ChatHistorySendTextBinding.Bot,
            MessageMode = ChatHistoryMessageModeBinding.Auto // Always Auto for bot replies
        });
    }

    private async Task RecordLLMChatHistory(WebhookEvent webhookEvent, Guid chatSessionId,  
        ChatCompletionModels.ChatCompletionResponse llmResponse)
    {
        _logger.LogInformation("Bot reply message: {BotReplyMessage}", llmResponse.OutputCompletion);

        await _chatHistoryRepository.AddAsync(new ChatHistory
        {
            Id = Guid.NewGuid(),
            ChatSessionId = chatSessionId,
            WebhookEventId = webhookEvent.Id,
            Message = llmResponse.OutputCompletion,
            SenderType = ChatHistorySendTextBinding.Bot,
            MessageMode = ChatHistoryMessageModeBinding.Auto, // Always Auto for bot replies
            LLMsInput = llmResponse.InputPrompt,
            LLMsProcessingTime = llmResponse.ProcessingTime,
            LLMsInputToken = llmResponse.InputToken,
            LLMsOutputToken = llmResponse.OutputToken  
        });
    }

    private string GetGreetingMessageFromChatSelectionMode(string message)
    {
        switch (message)
        {
            case ChatModeConstants.Constants.SelectAgent:
                return "สวัสดีครับ Agent AI ขออนุญาตเริ่มต้นการสนทนาใหม่นะครับ ข้อความก่อนหน้านี้จะไม่ถูกนำมาประมวลผล";
            case ChatModeConstants.Constants.SelectHumanAdmin:
                return "สวัสดีครับ Admin (คน) พร้อมตอบคำถามที่คุณกำลังสนทนาครับ";
            default:
                return string.Empty;
        }
    }

    private async Task<User> RegisterLineUserToTheSystem(LineMessagingAPI.WebhookRequest.Event lineEvent, string accessToken, WebhookEvent webhookEvent)
    {
        // Get the user profile from LINE API
        if (lineEvent.Source == null || string.IsNullOrEmpty(lineEvent.Source.UserId))
        {
            _logger.LogError("Event source or UserId is null when trying to retrieve LINE profile.");
            throw new ArgumentException("Event source or UserId is required.");
        }
        var lineProfile = await _lineInfraService.GetLineProfile(accessToken, lineEvent.Source.UserId);
        if (lineProfile == null)
        {
            _logger.LogError("Failed to retrieve LINE profile for UserId: {UserId}", lineEvent.Source.UserId);
            throw new InvalidOperationException("Failed to retrieve LINE profile.");
        }

        // Insert new user from the event if it doesn't exist
        var existingUser = await _userBusinessService.AddOrUpdateAsync(new User
        {
            Id = Guid.NewGuid(),
            LineUserId = lineEvent.Source.UserId,
            WebhookEventId = webhookEvent.Id,
            LineDisplayName = lineProfile.DisplayName ?? string.Empty
        });

        return existingUser;
    }

    private async Task<WebhookEvent> RecordWebhookFromLineEvent(LineMessagingAPI.WebhookRequest.Event lineEvent)
    {
        var source = lineEvent.Source;
        var webhookEvent = new WebhookEvent
        {
            EventJson = JsonSerializer.Serialize(lineEvent),
            EventType = lineEvent.Type,
            CreatedAt = DateTimeOffset.UtcNow,
            Processed = false,
            LineWebhookEventId = lineEvent.WebhookEventId,
            SourceType = source?.Type,
            GroupId = source?.GroupId,
            UserId = source?.UserId,
            ReplyToken = lineEvent.ReplyToken
        };

        await _webhookEventRepository.AddAsync(webhookEvent);
        return webhookEvent;
    }

    private async Task MarkWebhookEventProcessedAsync(WebhookEvent webhookEvent)
    {
        webhookEvent.Processed = true;
        webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
        await _webhookEventRepository.UpdateAsync(webhookEvent);
    }

    private async Task HandleLineEventErrorAsync(Exception ex, WebhookEvent webhookEvent)
    {
        _logger.LogError(ex, "Error processing LINE event");
        webhookEvent.ErrorMessage = ex.StackTrace ?? ex.Message;
        // Do not set as processed, just update the event with the error
        await _webhookEventRepository.UpdateAsync(webhookEvent);
    }

    private static ChatModeConstants.Enums GetChatMode(string? messageText)
    {
        var chatMode = ChatModeConstants.Enums.ContinuteChat;
        switch (messageText)
        {
            case ChatModeConstants.Constants.SelectAgent:
                chatMode = ChatModeConstants.Enums.SelectAgent;
                break;
            case ChatModeConstants.Constants.SelectHumanAdmin:
                chatMode = ChatModeConstants.Enums.SelectHumanAdmin;
                break;
        }

        return chatMode;
    }

    private async Task<Guid> SetUserChatMode(ChatModeConstants.Enums chatMode, WebhookEvent webhookEvent, User user, string? messageText)
    {
        if (chatMode == ChatModeConstants.Enums.ContinuteChat && user.LatestChatSessionId.HasValue)
        {
            // If the chat mode is to continue the chat, return the latest chat session ID
            return user.LatestChatSessionId ?? Guid.Empty;
        }

        var replyMode = ChatSessionReplyModeBinding.AutoReplyByAI;
        if (chatMode == ChatModeConstants.Enums.SelectHumanAdmin)
        {
            replyMode = ChatSessionReplyModeBinding.ManualReplyByAdmin;
        }

        var session = await _chatSessionRepository.AddAsync(new ChatSession
        {
            UserId = user.Id,
            WebhookEventId = webhookEvent.Id,
            ReplyMode = replyMode
        });

        await _userBusinessService.UpdateLatestChatSessionAsync(user.Id, session.Id, replyMode);

        return session.Id;
    }

    private sealed class LineEventInfo
    {
        public string? MessageText { get; }
        public string? ReplyToken { get; }
        public string? UserId { get; }

        public LineEventInfo(LineMessagingAPI.WebhookRequest.Event lineEvent)
        {
            MessageText = lineEvent.Message?.Text;
            ReplyToken = lineEvent.ReplyToken;
            UserId = lineEvent.Source?.UserId;
        }
    }

    private bool IsGreetingMessage(string botReplyMessage)
    {
        return !string.IsNullOrEmpty(botReplyMessage);
    }

    private bool IsAIAutoReply(User existingUser)
    {
        // Check if the user has a reply mode set and if it is set to AutoReplyByAI
        return !string.IsNullOrEmpty(existingUser.ReplyMode) &&
               existingUser.ReplyMode == ChatSessionReplyModeBinding.AutoReplyByAI;
    }

    private async Task HandleAIAutoReplyAsync(WebhookEvent webhookEvent, Guid chatSessionId,
        string botReplyMessage, string accessToken, string replyToken)
    {
        // If a specific greeting message is set, record it as the bot reply
        await RecordBotChatHistory(webhookEvent, chatSessionId, botReplyMessage);

        // Send the message back to the user
        await ReplyToLineUserAsync(accessToken, botReplyMessage, replyToken);

        // Mark the webhook event as processed
        await MarkWebhookEventProcessedAsync(webhookEvent);
    }
    
    private async Task HandleAILLMsReplyAsync(WebhookEvent webhookEvent, Guid chatSessionId,
        string accessToken, string replyToken, ChatCompletionModels.ChatCompletionResponse llmResponse)
    {
        // If a specific greeting message is set, record it as the bot reply
        await RecordLLMChatHistory(webhookEvent, chatSessionId, llmResponse);

        // Send the message back to the user
        await ReplyToLineUserAsync(accessToken, llmResponse.OutputCompletion, replyToken);

        // Mark the webhook event as processed
        await MarkWebhookEventProcessedAsync(webhookEvent);
    }
}