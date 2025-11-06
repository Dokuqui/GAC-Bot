using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Linq;

namespace MyGamingBot.Features.AI;

public class AiCommands : ApplicationCommandModule
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _geminiApiKey;
    private readonly string _geminiApiUrl;
    private readonly string _searchApiKey;
    private readonly string _searchCx;

    public AiCommands(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;

        _geminiApiKey = config.GetValue<string>("GoogleAi:Key")!;
        _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

        _searchApiKey = config.GetValue<string>("GoogleSearch:Key")!;
        _searchCx = config.GetValue<string>("GoogleSearch:Cx")!;

        if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrEmpty(_searchApiKey) || string.IsNullOrEmpty(_searchCx))
        {
            throw new Exception("One or more API keys (Gemini or Search) are not set in user secrets!");
        }
    }

    [SlashCommand("ask", "Ask the bot an up-to-date question.")]
    public async Task AskAi(InteractionContext ctx,
        [Option("question", "Your question for the AI.")] string question)
    {
        await ctx.DeferAsync();

        try
        {
            string searchContext = await PerformGoogleSearchAsync(question);

            var prompt = $"""
            You are a helpful assistant. Please answer the user's question based on the following up-to-date search results.
            Synthesize the information from all the snippets to build the best possible answer.
            If the snippets are confusing or don't answer the question, you can state that.

            Search Results:
            {searchContext}

            User's Question:
            {question}
            """;

            var client = _httpClientFactory.CreateClient();
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };
            
            string jsonPayload = JsonConvert.SerializeObject(requestBody);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_geminiApiUrl, httpContent);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"❌ The AI is not responding. (Details: {jsonResponse})"));
                return;
            }
            
            dynamic data = JsonConvert.DeserializeObject(jsonResponse)!;
            string answer = data.candidates[0].content.parts[0].text;

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"❓ Your Question: {question}")
                .WithDescription(answer)
                .WithColor(DiscordColor.Blurple)
                .WithFooter("Powered by Google AI + Google Search");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"⚠️ An error occurred: {ex.Message}"));
        }
    }

    private async Task<string> PerformGoogleSearchAsync(string query)
    {
        var client = _httpClientFactory.CreateClient();

        string url = $"https://www.googleapis.com/customsearch/v1?key={_searchApiKey}&cx={_searchCx}&q={Uri.EscapeDataString(query)}";

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return "Error: Could not perform a web search.";
        }

        string json = await response.Content.ReadAsStringAsync();
        dynamic data = JsonConvert.DeserializeObject(json)!;

        var sb = new StringBuilder();

        foreach (var item in ((IEnumerable<dynamic>)data.items).Take(3))
        {
            sb.AppendLine($"Source: {item.title}");
            sb.AppendLine($"Snippet: {item.snippet}");
            sb.AppendLine();
        }

        if (sb.Length == 0)
        {
            return "No relevant information found on the web.";
        }

        return sb.ToString();
    }
}