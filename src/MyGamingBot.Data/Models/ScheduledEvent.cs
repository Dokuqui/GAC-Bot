using System.ComponentModel.DataAnnotations;

namespace MyGamingBot.Data.Models;

public class ScheduledEvent
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong CreatorId { get; set; }
    public string Game { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}