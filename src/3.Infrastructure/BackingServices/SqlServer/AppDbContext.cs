using Microsoft.EntityFrameworkCore;
using AgentAI.Domain;

namespace AgentAI.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ChatHistory> ChatHistories { get; set; }
    public DbSet<UserReplyToken> UserReplyTokens { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
