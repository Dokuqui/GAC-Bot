using Microsoft.EntityFrameworkCore;
using MyGamingBot.Data;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Features.Leaderboard;

public class LeaderboardService
{
    private readonly BotDbContext _db;

    public LeaderboardService(BotDbContext dbContext)
    {
        _db = dbContext;
    }

    public async Task GivePointAsync(ulong guildId, ulong userId)
    {
        var entry = await _db.LeaderboardEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == userId);

        if (entry == null)
        {
            entry = new LeaderboardEntry
            {
                GuildId = guildId,
                UserId = userId,
                Points = 1
            };
            await _db.LeaderboardEntries.AddAsync(entry);
        }
        else
        {
            entry.Points++;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(ulong guildId)
    {
        return await _db.LeaderboardEntries
            .Where(e => e.GuildId == guildId)
            .OrderByDescending(e => e.Points)
            .Take(5)
            .ToListAsync();
    }
}