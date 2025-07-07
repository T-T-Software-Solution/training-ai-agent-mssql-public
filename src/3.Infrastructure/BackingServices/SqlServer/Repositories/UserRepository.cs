using Microsoft.EntityFrameworkCore;
using AgentAI.Domain;

namespace AgentAI.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByLineIdAsync(string LineUserId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.LineUserId == LineUserId);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        user.Id = Guid.NewGuid(); // Ensure a new ID is generated
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task UpdateLatestChatSessionAsync(Guid userId, Guid chatSessionId, string replyMode)
    {
        var existing = await _context.Users.FindAsync(userId);
        if (existing != null)
        {
            existing.LatestChatSessionId = chatSessionId;
            existing.ReplyMode = replyMode;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Users.FindAsync(id);
        if (entity != null)
        {
            _context.Users.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
