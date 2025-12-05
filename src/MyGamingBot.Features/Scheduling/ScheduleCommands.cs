using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MyGamingBot.Data;
using MyGamingBot.Data.Models;
using System.Globalization;
using System.Text;

namespace MyGamingBot.Features.Scheduling;

[SlashCommandGroup("schedule", "Commands for scheduling game nights.")]
public class ScheduleCommands : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScheduleCommands(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var newEvent = new ScheduledEvent
            {
                GuildId = ctx.Guild.Id,
                CreatorId = ctx.User.Id,
                Game = game,
                Description = description ?? "No description provided.",
                StartTime = startTime
            };

            await db.ScheduledEvents.AddAsync(newEvent);
            await db.SaveChangesAsync();
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("✅ Event Saved to Database!")
            .WithDescription($"I've scheduled **{game}**.")
            .WithColor(DiscordColor.Green)
            .AddField("Time", $"<t:{new DateTimeOffset(startTime).ToUnixTimeSeconds()}:F>")
            .AddField("Description", description ?? "No description provided.");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("list", "List upcoming events from the database.")]
    public async Task ListSchedules(InteractionContext ctx)
    {
        await ctx.DeferAsync();

        List<ScheduledEvent> futureEvents;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            
            futureEvents = await db.ScheduledEvents
                .Where(e => e.GuildId == ctx.Guild.Id && e.StartTime > DateTime.Now)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }

        if (futureEvents.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("ℹ️ No upcoming events found in the database."));
            return;
        }

        var sb = new StringBuilder();
        foreach (var ev in futureEvents)
        {
            long unixTime = new DateTimeOffset(ev.StartTime).ToUnixTimeSeconds();
            sb.AppendLine($"**{ev.Game}**");
            sb.AppendLine($"⏰ <t:{unixTime}:F> (<t:{unixTime}:R>)");
            sb.AppendLine($"📝 {ev.Description}");
            sb.AppendLine("------------------");
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📅 Upcoming Game Events")
            .WithDescription(sb.ToString())
            .WithColor(DiscordColor.Blurple)
            .WithFooter($"Found {futureEvents.Count} events in the database.");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
    
    [SlashCommand("clear-old", "Delete events that have already passed.")]
    public async Task ClearOldEvents(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            
            var oldEvents = await db.ScheduledEvents
                .Where(e => e.GuildId == ctx.Guild.Id && e.StartTime < DateTime.Now)
                .ToListAsync();
                
            if (oldEvents.Count > 0)
            {
                db.ScheduledEvents.RemoveRange(oldEvents);
                await db.SaveChangesAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"🗑️ Cleaned up {oldEvents.Count} old events from the database."));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("✨ No old events to clean up."));
            }
        }
    }
}