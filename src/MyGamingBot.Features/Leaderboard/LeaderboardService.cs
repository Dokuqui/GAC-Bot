using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyGamingBot.Data;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Features.Leaderboard;

public class LeaderboardService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LeaderboardService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task GivePointAsync(ulong guildId, ulong userId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            
            var entry = await db.LeaderboardEntries
                .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == userId);

            if (entry == null)
            {
                entry = new LeaderboardEntry
                {
                    GuildId = guildId,
                    UserId = userId,
                    Points = 1
                };
                await db.LeaderboardEntries.AddAsync(entry);
            }
            else
            {
                entry.Points++;
            }

            await db.SaveChangesAsync();
        }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(ulong guildId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            return await db.LeaderboardEntries
                .Where(e => e.GuildId == guildId)
                .OrderByDescending(e => e.Points)
                .Take(5)
                .ToListAsync();
        }
    }
}