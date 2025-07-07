using System.ComponentModel.DataAnnotations;

namespace AgentAI.Domain;


public class BaseEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}



