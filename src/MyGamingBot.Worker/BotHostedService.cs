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
    private readonly IConfiguration _config;

    public BotHostedService(ILogger<BotHostedService> logger, DiscordClient discord, IConfiguration config)
    {
        _logger = logger;
        _discord = discord;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bot is starting...");

        _discord.ComponentInteractionCreated += OnComponentInteractionCreated;
        _discord.GuildMemberAdded += OnGuildMemberAdded;

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

    private async Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        _logger.LogInformation($"New member joined: {e.Member.Username} in guild {e.Guild.Name}");

        ulong roleId = _config.GetValue<ulong>("GuildSettings:WelcomeRole");
        if (roleId == 0)
        {
            _logger.LogError("WelcomeRole is not set in configuration!");
            return;
        }

        DiscordRole? role = e.Guild.GetRole(roleId);
        if (role == null)
        {
            _logger.LogError($"Could not find role with ID {roleId} in guild {e.Guild.Name}");
            return;
        }

        try
        {
            await e.Member.GrantRoleAsync(role, "New member role assignment");
            _logger.LogInformation($"Assigned role '{role.Name}' to {e.Member.Username}");

            var dmEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Welcome to {e.Guild.Name}!")
                .WithDescription($"Hi {e.Member.Mention}! We're glad to have you. Please take a moment to read the server rules.")
                .WithColor(DiscordColor.Green)
                .WithThumbnail(e.Guild.IconUrl);

            await e.Member.SendMessageAsync(dmEmbed);
            _logger.LogInformation($"Sent welcome DM to {e.Member.Username}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to assign role or DM new user {e.Member.Username}");
        }
    }
}