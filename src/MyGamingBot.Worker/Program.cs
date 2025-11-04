using System.Reflection;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyGamingBot.Features;
using MyGamingBot.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        string token = context.Configuration.GetValue<string>("Discord:Token")!;
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Discord:Token is not set in user secrets!");
        }

        services.AddSingleton(new DiscordClient(new DiscordConfiguration
        {
            Token = token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages
        }));

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
        services.AddHostedService<BotHostedService>();
    })
    .Build();

var slashCommands = host.Services.GetRequiredService<SlashCommandsExtension>();
var featuresModule = host.Services.GetRequiredService<FeaturesModule>();

slashCommands.RegisterCommands(Assembly.GetAssembly(typeof(FeaturesModule))!);

await host.RunAsync();