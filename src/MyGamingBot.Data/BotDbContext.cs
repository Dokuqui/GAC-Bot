using Microsoft.EntityFrameworkCore;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Data;

public class BotDbContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }

    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }
}