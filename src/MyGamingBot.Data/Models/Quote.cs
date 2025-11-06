using System.ComponentModel.DataAnnotations;

namespace MyGamingBot.Data.Models;

public class Quote
{
    [Key]
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}