using Microsoft.EntityFrameworkCore;
using AgentAI.Domain;

namespace AgentAI.Infrastructure;

public class ChatSessionRepository : IChatSessionRepository
{
    private readonly AppDbContext _context;

    public ChatSessionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession?> GetByIdAsync(Guid id)
    {
        return await _context.ChatSessions.FindAsync(id);
    }

    public async Task<IEnumerable<ChatSession>> GetAllAsync()
    {
        return await _context.ChatSessions.ToListAsync();
    }

    public async Task<ChatSession> AddAsync(ChatSession chatSession)
    {
        chatSession.Id = Guid.NewGuid(); // Ensure a new ID is generated
        await _context.ChatSessions.AddAsync(chatSession);
        await _context.SaveChangesAsync();

        return chatSession;
    }

    public async Task UpdateAsync(ChatSession chatSession)
    {
        var existing = await _context.ChatSessions.FindAsync(chatSession.Id);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(chatSession);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.ChatSessions.FindAsync(id);
        if (entity != null)
        {
            _context.ChatSessions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
