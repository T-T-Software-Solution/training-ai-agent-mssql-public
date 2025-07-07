namespace AgentAI.Application;

public interface ILineMessagingInfraService
{
    Task<string> LineLoginAsync();
    Task<bool> SendMessageAsync(string accessToken, string message, string replyTokenString);
    bool VerifySignature(string requestBody, string? signature);
    string GenerateSignature(string body);
    Task LineLoading(string accessToken, string userId);
    Task<LineMessagingAPI.UserProfile> GetLineProfile(string accessToken, string userId);
    Task<LineMessagingAPI.ReplyMessage> PushMessageAsync(string accessToken, string message, string toUserId);
}
