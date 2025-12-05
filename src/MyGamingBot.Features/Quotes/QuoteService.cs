using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyGamingBot.Data;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Features.Quotes;

public class QuoteService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Random _random;

    public QuoteService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _random = new Random();
    }

    public async Task AddQuoteAsync(ulong guildId, string author, string text)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            
            var newQuote = new Quote
            {
                GuildId = guildId,
                Author = author,
                Text = text,
                AddedAt = DateTime.UtcNow
            };

            await db.Quotes.AddAsync(newQuote);
            await db.SaveChangesAsync();
        }
    }

    public async Task<Quote?> GetRandomQuoteAsync(ulong guildId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var count = await db.Quotes.CountAsync(q => q.GuildId == guildId);
            if (count == 0) return null;

            int index = _random.Next(0, count);

            return await db.Quotes
                .Where(q => q.GuildId == guildId)
                .OrderBy(q => q.Id)
                .Skip(index)
                .FirstOrDefaultAsync();
        }
    }
}