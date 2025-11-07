using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MyGamingBot.Data.Models;

namespace MyGamingBot.Features.Quotes;

[SlashCommandGroup("quote", "Save and view server quotes.")]
public class QuoteCommands : ApplicationCommandModule
{
    private readonly QuoteService _quoteService;

    public QuoteCommands(QuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [SlashCommand("add", "Add a new quote to the quote book.")]
    public async Task AddQuote(InteractionContext ctx,
        [Option("text", "The quote text.")] string text,
        [Option("author", "Who said it (use a @mention or just text).")] string author)
    {
        await _quoteService.AddQuoteAsync(ctx.Guild.Id, author, text);

        var embed = new DiscordEmbedBuilder()
            .WithTitle("✅ Quote Added!")
            .WithDescription($"\"{text}\"\n- {author}")
            .WithColor(DiscordColor.Green);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }

    [SlashCommand("random", "Get a random quote from the book.")]
    public async Task GetRandomQuote(InteractionContext ctx)
    {
        var quote = await _quoteService.GetRandomQuoteAsync(ctx.Guild.Id);

        if (quote == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("There are no quotes saved yet! Use `/quote add`.").AsEphemeral(true));
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"\"{quote.Text}\"")
            .WithDescription($"- {quote.Author}")
            .WithFooter($"Added on {quote.AddedAt.ToShortDateString()}")
            .WithColor(DiscordColor.Blurple);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}