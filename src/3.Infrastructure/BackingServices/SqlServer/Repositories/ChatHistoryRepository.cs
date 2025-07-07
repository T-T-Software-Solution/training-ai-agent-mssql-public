using Microsoft.EntityFrameworkCore;
using AgentAI.Domain;
using AgentAI.Application;

namespace AgentAI.Infrastructure;

public class ChatHistoryRepository : IChatHistoryRepository
{
    private readonly AppDbContext _context;

    public ChatHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatHistory?> GetByIdAsync(Guid id)
    {
        return await _context.ChatHistories.FindAsync(id);
    }

    public async Task AddAsync(ChatHistory chatHistory)
    {
        chatHistory.Id = Guid.NewGuid();
        await _context.ChatHistories.AddAsync(chatHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var chat = await _context.ChatHistories.FindAsync(id);
        if (chat != null)
        {
            _context.ChatHistories.Remove(chat);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<ChatHistory>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.ChatHistories
            // Filter chat histories by session ID
            .Where(ch => ch.ChatSessionId == sessionId &&
                    (
                        // Include only messages sent by bot
                        // and only manual messages from the user
                        ch.SenderType == ChatHistorySendTextBinding.Bot ||
                        (
                            ch.SenderType == ChatHistorySendTextBinding.User &&
                            ch.MessageMode == ChatHistoryMessageModeBinding.Manual
                        )
                    )
                )
            .OrderBy(ch => ch.CreatedAt)
            .ToListAsync();
    }
}
