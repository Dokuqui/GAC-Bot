using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;

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

        _discord.ComponentInteractionCreated += OnComponentInteractionCreated;

        await _discord.ConnectAsync();
        _logger.LogInformation("Bot is connected.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot is stopping...");

        _discord.ComponentInteractionCreated -= OnComponentInteractionCreated;

        await _discord.DisconnectAsync();
        _logger.LogInformation("Bot is disconnected.");
    }

    private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id != "lfg_join" && e.Id != "lfg_leave")
        {
            return;
        }

        var originalEmbed = e.Message.Embeds.FirstOrDefault();
        if (originalEmbed == null || originalEmbed.Fields.Count == 0)
        {
            return;
        }

        var originalField = originalEmbed.Fields[0];

        var playerMentions = originalField.Value.Split('\n')
                                        .Where(s => s.StartsWith("<@"))
                                        .ToList();

        int groupSize = int.Parse(originalField.Name.Split('/')[1].Replace(")", ""));

        bool userInList = playerMentions.Contains(e.User.Mention);

        if (e.Id == "lfg_join")
        {
            if (userInList)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You're already in this LFG!").AsEphemeral(true));
                return;
            }

            if (playerMentions.Count >= groupSize)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                   new DiscordInteractionResponseBuilder().WithContent("This LFG is full!").AsEphemeral(true));
                return;
            }

            playerMentions.Add(e.User.Mention);
        }
        else if (e.Id == "lfg_leave")
        {
            if (!userInList)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You aren't in this LFG.").AsEphemeral(true));
                return;
            }

            playerMentions.Remove(e.User.Mention);
        }

        var playerListString = new StringBuilder();
        if (playerMentions.Count == 0)
        {
            playerListString.AppendLine("No one has joined yet.");
        }
        else
        {
            foreach (var user in playerMentions)
                playerListString.AppendLine(user);
        }

        var newEmbed = new DiscordEmbedBuilder(originalEmbed)
            .ClearFields()
            .AddField($"Players ({playerMentions.Count}/{groupSize})", playerListString.ToString());

        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(newEmbed)
                .AddComponents(e.Message.Components));
    }
}