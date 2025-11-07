using System.Reflection;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyGamingBot.Features;
using MyGamingBot.Worker;
using MyGamingBot.Data;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        string token = context.Configuration.GetValue<string>("Discord:Token")!;
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Discord:Token is not set in user secrets!");
        }

        services.AddDbContext<BotDbContext>(options =>
        {
            options.UseSqlite("Data Source=mybot.db");
        });

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages
        });

        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2)
        });

        services.AddSingleton(discord);

        services.AddHttpClient();

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<DiscordClient>();
            var slash = client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = sp
            });
            return slash;
        });

        services.AddSingleton<FeaturesModule>();

        services.AddSingleton<MyGamingBot.Features.Polling.PollCommands>();

        services.AddSingleton<MyGamingBot.Features.LFG.LfgCommands>();

        services.AddSingleton<MyGamingBot.Features.Quotes.QuoteService>();
        services.AddSingleton<MyGamingBot.Features.Quotes.QuoteCommands>();

        services.AddSingleton<MyGamingBot.Features.Leaderboard.LeaderboardService>();
        services.AddSingleton<MyGamingBot.Features.Leaderboard.LeaderboardCommands>();

        services.AddSingleton<MyGamingBot.Features.AI.AiCommands>();

        services.AddSingleton<MyGamingBot.Features.Scheduling.ScheduleCommands>();
        
        services.AddHostedService<BotHostedService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    dbContext.Database.Migrate();
}

var slashCommands = host.Services.GetRequiredService<SlashCommandsExtension>();
var featuresModule = host.Services.GetRequiredService<FeaturesModule>();

slashCommands.RegisterCommands(Assembly.GetAssembly(typeof(FeaturesModule))!);

await host.RunAsync();
