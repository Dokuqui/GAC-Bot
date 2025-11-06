using Newtonsoft.Json;

namespace MyGamingBot.Features.Quotes;


public class QuoteService
{
    private readonly List<Quote> _quotes;
    private const string FilePath = "quotes.json";
    private readonly Random _random;

    public QuoteService()
    {
        _quotes = LoadQuotes();
        _random = new Random();
    }

    private List<Quote> LoadQuotes()
    {
        if (!File.Exists(FilePath))
        {
            File.WriteAllText(FilePath, "[]");
            return new List<Quote>();
        }

        string json = File.ReadAllText(FilePath);
        return JsonConvert.DeserializeObject<List<Quote>>(json) ?? new List<Quote>();
    }

    private async Task SaveQuotesAsync()
    {
        string json = JsonConvert.SerializeObject(_quotes, Formatting.Indented);
        await File.WriteAllTextAsync(FilePath, json);
    }

    public async Task AddQuoteAsync(string author, string text)
    {
        var newQuote = new Quote
        {
            Author = author,
            Text = text,
            AddedAt = DateTime.UtcNow
        };

        _quotes.Add(newQuote);
        await SaveQuotesAsync();
    }

    public Quote? GetRandomQuote()
    {
        if (_quotes.Count == 0)
        {
            return null;
        }

        int index = _random.Next(0, _quotes.Count);
        return _quotes[index];
    }
}