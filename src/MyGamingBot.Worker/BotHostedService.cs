using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyGamingBot.Worker;

public class BotHostedService : IHostedService
{
    private readonly ILogger<BotHostedService> _logger;
    private readonly DiscordClient _discord;

    public BotHostedService(ILogger<BotHostedService> logger, DiscordClient discord)
    {
        _logger = logger;
        _discord = discord;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    _logger.LogInformation("Bot is starting...");

    await _discord.ConnectAsync();

    _logger.LogInformation("Bot is connected.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot is stopping...");

        await _discord.DisconnectAsync();

        _logger.LogInformation("Bot is disconnected.");
    }
}