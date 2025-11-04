using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace MyGamingBot.Features.LFG;

[SlashCommandGroup("lfg", "Create and manage Looking-for-Group posts.")]
public class LfgCommands : ApplicationCommandModule
{
    [SlashCommand("create", "Create a new LFG post.")]
    public async Task CreateLfg(InteractionContext ctx,
        [Option("game", "The game you want to play.")] string game,
        [Option("time", "When you want to play (e.g., '9pm EST', 'in 1 hour').")] string time,
        [Option("size", "The total size of the group (e.g., 4).")] long groupSize)
    {
        var joinButton = new DiscordButtonComponent(ButtonStyle.Success, "lfg_join", "Join");
        var leaveButton = new DiscordButtonComponent(ButtonStyle.Danger, "lfg_leave", "Leave");

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"LFG for {game}!")
            .WithDescription($"**Time:** {time}\n**Creator:** {ctx.User.Mention}")
            .AddField($"Players (1/{groupSize})", ctx.User.Mention)
            .WithColor(new DiscordColor("2ECC71"));

        var builder = new DiscordInteractionResponseBuilder()
            .AddEmbed(embed)
            .AddComponents(joinButton, leaveButton);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
    }
}