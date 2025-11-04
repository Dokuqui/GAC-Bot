using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text;

namespace MyGamingBot.Features.Polling;

[SlashCommandGroup("poll", "Commands for creating and managing polls.")]
public class PollCommands : ApplicationCommandModule
{
    [SlashCommand("create", "Create a new poll for your friends.")]
    public async Task CreatePoll(InteractionContext ctx,
        [Option("question", "The question you want to ask.")] string question,
        [Option("choice1", "The first choice.")] string choice1,
        [Option("choice2", "The second choice.")] string choice2,
        [Option("choice3", "The third choice (optional).")] string? choice3 = null,
        [Option("choice4", "The fourth choice (optional).")] string? choice4 = null,
        [Option("choice5", "The fifth choice (optional).")] string? choice5 = null
    )
    {
        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle($"POLL: {question}")
            .WithColor(new DiscordColor("5865F2"));

        var description = new StringBuilder();
        description.AppendLine($":one: {choice1}");
        description.AppendLine($":two: {choice2}");

        if (!string.IsNullOrEmpty(choice3))
            description.AppendLine($":three: {choice3}");
        if (!string.IsNullOrEmpty(choice4))
            description.AppendLine($":four: {choice4}");
        if (!string.IsNullOrEmpty(choice5))
            description.AppendLine($":five: {choice5}");

        embedBuilder.WithDescription(description.ToString());

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder).AsEphemeral(false));

        var responseMessage = await ctx.GetOriginalResponseAsync();

        await responseMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":one:"));
        await responseMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":two:"));

        if (!string.IsNullOrEmpty(choice3))
            await responseMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":three:"));
        if (!string.IsNullOrEmpty(choice4))
            await responseMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":four:"));
        if (!string.IsNullOrEmpty(choice5))
            await responseMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":five:"));
    }
}