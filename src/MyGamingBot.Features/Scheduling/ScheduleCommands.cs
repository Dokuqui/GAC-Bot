using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Globalization;

namespace MyGamingBot.Features.Scheduling;

[SlashCommandGroup("schedule", "Commands for scheduling game nights.")]
public class ScheduleCommands : ApplicationCommandModule
{
    private static readonly List<ScheduledEvent> _events = new();

    private class ScheduledEvent
    {
        public string Game { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string? Description { get; set; }
    }

    [SlashCommand("create", "Schedule a new game night event.")]
    public async Task CreateSchedule(InteractionContext ctx,
        [Option("game", "The name of the game or event.")] string game,
        [Option("date", "The date in YYYY-MM-DD format (e.g., 2025-11-07).")] string date,
        [Option("time", "The time in 24-hour format (e.g., 21:00).")] string time,
        [Option("description", "A short description for the event.")] string? description = null)
    {
        await ctx.DeferAsync();

        string dateTimeString = $"{date} {time}";
        if (!DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("❌ **Error:** Invalid date or time format. Please use `YYYY-MM-DD` and `HH:mm`."));
            return;
        }

        if (startTime < DateTime.Now)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("❌ **Error:** You can't schedule an event in the past!"));
            return;
        }

        _events.Add(new ScheduledEvent
        {
            Game = game,
            StartTime = startTime,
            Description = description
        });

        var embed = new DiscordEmbedBuilder()
            .WithTitle("✅ Event Scheduled!")
            .WithDescription($"I've scheduled the **{game}** event for you.")
            .WithColor(DiscordColor.Green)
            .AddField("Time", $"<t:{new DateTimeOffset(startTime).ToUnixTimeSeconds()}:F>")
            .AddField("Description", description ?? "No description provided.");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("list", "List all upcoming scheduled events.")]
    public async Task ListSchedules(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        if (_events.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("ℹ️ No events scheduled."));
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📅 Upcoming Game Events")
            .WithColor(DiscordColor.Blurple);

        foreach (var ev in _events.OrderBy(e => e.StartTime))
        {
            embed.AddField($"{ev.Game} — <t:{new DateTimeOffset(ev.StartTime).ToUnixTimeSeconds()}:F>",
                           ev.Description ?? "No description");
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}
