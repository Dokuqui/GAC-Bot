using Microsoft.EntityFrameworkCore;
using MyGamingBot.Data;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Features.Quotes;

public class QuoteService
{
    private readonly BotDbContext _db;
    private readonly Random _random;

    public QuoteService(BotDbContext dbContext)
    {
        _db = dbContext;
        _random = new Random();
    }

    public async Task AddQuoteAsync(ulong guildId, string author, string text)
    {
        var newQuote = new Quote
        {
            GuildId = guildId,
            Author = author,
            Text = text,
            AddedAt = DateTime.UtcNow
        };

        await _db.Quotes.AddAsync(newQuote);

        await _db.SaveChangesAsync();
    }

    public async Task<Quote?> GetRandomQuoteAsync(ulong guildId)
    {
        var count = await _db.Quotes.CountAsync(q => q.GuildId == guildId);
        if (count == 0)
        {
            return null;
        }

        int index = _random.Next(0, count);

        return await _db.Quotes
            .Where(q => q.GuildId == guildId)
            .OrderBy(q => q.Id)
            .Skip(index)
            .FirstOrDefaultAsync();
    }
}