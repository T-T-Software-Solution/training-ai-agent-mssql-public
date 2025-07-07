using AgentAI.Domain;
using Microsoft.Extensions.Logging;

namespace AgentAI.Application;

public interface IUserBusinessService
{
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddOrUpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task SaveLineReplyTokenAsync(string lineReplyToken, Guid userId, Guid webhookEventId);
    Task<List<string>> GetAvailableReplyTokenAsync(Guid userId);
    Task UpdateLatestChatSessionAsync(Guid userId, Guid chatSessionId, string replyMode);
}

public class UserBusinessService : IUserBusinessService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserBusinessService> _logger;
    private readonly IUserReplyTokenRepository _userReplyTokenRepository;

    public UserBusinessService(IUserRepository repository, ILogger<UserBusinessService> logger,
        IUserReplyTokenRepository userReplyTokenRepository)
    {
        _repository = repository;
        _logger = logger;
        _userReplyTokenRepository = userReplyTokenRepository;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with id {Id} not found.", id);
        }
        return user;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<User> AddOrUpdateAsync(User user)
    {
        // Check if the user already exists by LineUserId
        var existingUser = await _repository.GetByLineIdAsync(user.LineUserId);

        // If the user exists, do not add it again
        if (existingUser != null)
            return existingUser;

        var userInserted = await _repository.AddAsync(user);

        _logger.LogInformation("Added user with id {Id}.", user.Id);

        return userInserted;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Deleted user with id {Id}.", id);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null;
    }

    public async Task SaveLineReplyTokenAsync(string lineReplyToken, Guid userId, Guid webhookEventId)
    {
        if (string.IsNullOrEmpty(lineReplyToken))
        {
            throw new ArgumentNullException(nameof(lineReplyToken), "LineReplyToken cannot be null or empty");
        }

        // Create a new UserReplyToken
        var userReplyToken = new UserReplyToken
        {
            UserId = userId,
            WebhookEventId = webhookEventId,
            ReplyToken = lineReplyToken,
            IsProcessed = false
        };

        // Save the token to the repository
        await _userReplyTokenRepository.AddAsync(userReplyToken);
    }

    public async Task<List<string>> GetAvailableReplyTokenAsync(Guid userId)
    {
        return await _userReplyTokenRepository.GetAvailableReplyTokenAsync(userId);
    }

    public async Task UpdateLatestChatSessionAsync(Guid userId, Guid chatSessionId, string replyMode)
    {
        await _repository.UpdateLatestChatSessionAsync(userId, chatSessionId, replyMode);
        _logger.LogInformation("Updated LatestChatSessionId for user {UserId} to {ChatSessionId}.", userId, chatSessionId);
    }
}
