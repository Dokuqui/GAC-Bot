using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text;

namespace MyGamingBot.Features.Leaderboard;

public class LeaderboardCommands : ApplicationCommandModule
{
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardCommands(LeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [SlashCommand("givepoint", "Give a point to a user for being awesome.")]
    public async Task GivePoint(InteractionContext ctx,
        [Option("user", "The user to give a point to.")] DiscordUser user,
        [Option("reason", "The reason for giving the point (optional).")] string? reason = null)
    {
        if (user.Id == ctx.User.Id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("You can't give yourself a point!").AsEphemeral(true));
            return;
        }

        await _leaderboardService.GivePointAsync(ctx.Guild.Id, user.Id);

        string reasonText = string.IsNullOrEmpty(reason) ? "" : $" for {reason}";
        
        var embed = new DiscordEmbedBuilder()
            .WithTitle("✅ Point Awarded!")
            .WithDescription($"**{ctx.User.Username}** gave a point to **{user.Username}**{reasonText}!")
            .WithColor(DiscordColor.Gold);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }

    [SlashCommand("leaderboard", "Show the top 5 users with the most points.")]
    public async Task ShowLeaderboard(InteractionContext ctx)
    {
        var topUsers = await _leaderboardService.GetLeaderboardAsync(ctx.Guild.Id);

        if (topUsers.Count == 0)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("The leaderboard is empty! Use `/givepoint` to get started."));
            return;
        }

        var sb = new StringBuilder();
        var medals = new[] { "🥇", "🥈", "🥉", "4.", "5." };

        for (int i = 0; i < topUsers.Count; i++)
        {
            var entry = topUsers[i];
            var user = await ctx.Guild.GetMemberAsync(entry.UserId);
            var userName = user?.Username ?? $"User (ID: {entry.UserId})";

            sb.AppendLine($"**{medals[i]} {userName}** — {entry.Points} points");
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏆 Server Leaderboard")
            .WithDescription(sb.ToString())
            .WithColor(DiscordColor.Gold);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}