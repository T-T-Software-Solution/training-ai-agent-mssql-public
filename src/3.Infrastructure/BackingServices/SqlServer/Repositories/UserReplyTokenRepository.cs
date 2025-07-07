using Microsoft.EntityFrameworkCore;
using AgentAI.Domain;

namespace AgentAI.Infrastructure;

public class UserReplyTokenRepository : IUserReplyTokenRepository
{
    private readonly AppDbContext _context;

    public UserReplyTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserReplyToken?> GetByIdAsync(Guid id)
    {
        return await _context.UserReplyTokens.FindAsync(id);
    }

    public async Task<IEnumerable<UserReplyToken>> GetAllAsync()
    {
        return await _context.UserReplyTokens
            .OrderByDescending(u => u.ProcessedAt)
            .ToListAsync();
    }

    public async Task AddAsync(UserReplyToken userReplyToken)
    {
        userReplyToken.Id = Guid.NewGuid();
        await _context.UserReplyTokens.AddAsync(userReplyToken);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateReplyTokenAsProcessedAsync(string lineReplyToken, Guid userId)
    {
        var userReplyToken = await _context.UserReplyTokens.FirstOrDefaultAsync(c =>
            c.UserId == userId &&
            c.ReplyToken == lineReplyToken);

        if (userReplyToken != null)
        {
            userReplyToken.ProcessedAt = DateTime.UtcNow;
            userReplyToken.IsProcessed = true;

            _context.UserReplyTokens.Update(userReplyToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateReplyTokenAsProcessedAsync(List<string> lineReplyToken, Guid userId)
    {
        // Update multiple reply tokens as processed
        var userReplyTokens = await _context.UserReplyTokens
            .Where(c => c.UserId == userId && lineReplyToken.Contains(c.ReplyToken))
            .ToListAsync();
        if (userReplyTokens.Any())
        {
            foreach (var token in userReplyTokens)
            {
                token.ProcessedAt = DateTime.UtcNow;
                token.IsProcessed = true;
                _context.UserReplyTokens.Update(token);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetAvailableReplyTokenAsync(Guid userId)
    {
        return await _context.UserReplyTokens
            .Where(c => c.UserId == userId && !c.IsProcessed)
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.ReplyToken)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.UserReplyTokens.FindAsync(id);
        if (entity != null)
        {
            _context.UserReplyTokens.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
