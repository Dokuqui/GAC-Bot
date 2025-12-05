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
        _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_geminiApiKey}";

        _searchApiKey = config.GetValue<string>("GoogleSearch:Key")!;
        _searchCx = config.GetValue<string>("GoogleSearch:Cx")!;

        if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrEmpty(_searchApiKey) || string.IsNullOrEmpty(_searchCx))
        {
            throw new Exception("API keys (Gemini/Search) are missing!");
        }
    }

    [SlashCommand("ask", "Ask the bot any question and get up-to-date answers.")]
    public async Task AskAi(InteractionContext ctx,
        [Option("question", "Your question for the AI.")] string question)
    {
        await ctx.DeferAsync();

        try
        {
            // Step 1: Fetch up-to-date search results
            string searchContext = await PerformGoogleSearchAsync(question);

            // Step 2: Build AI prompt
            string prompt = $"""
            You are a helpful assistant. Answer the user's question based ONLY on the following up-to-date search results. 
            If the information is missing or unclear, say so.

            Search Results:
            {searchContext}

            User's Question:
            {question}
            """;

            // Step 3: Query AI
            string answer = await QueryAiModel(prompt);

            // Step 4: Send Discord embed response
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"❓ Question: {question}")
                .WithDescription(answer)
                .WithColor(DiscordColor.Blurple)
                .WithFooter("Powered by AI + Live Sources");

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
            return "No search results found.";

        string json = await response.Content.ReadAsStringAsync();
        dynamic data = JsonConvert.DeserializeObject(json)!;

        var sb = new StringBuilder();
        foreach (var item in ((IEnumerable<dynamic>)data.items).Take(5))
        {
            sb.AppendLine($"Source: {item.title}");
            sb.AppendLine($"Snippet: {item.snippet}");
            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : "No relevant search results.";
    }

    private async Task<string> QueryAiModel(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };
        var json = JsonConvert.SerializeObject(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_geminiApiUrl, httpContent);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        dynamic data = JsonConvert.DeserializeObject(jsonResponse)!;
        return data.candidates[0].content.parts[0].text;
    }
}
