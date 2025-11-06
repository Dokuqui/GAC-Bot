using System.ComponentModel.DataAnnotations;

namespace MyGamingBot.Data.Models;

public class LeaderboardEntry
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int Points { get; set; }
}