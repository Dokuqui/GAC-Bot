using DSharpPlus.SlashCommands;

namespace MyGamingBot.Features;

public class FeaturesModule : ApplicationCommandModule
{
    [SlashCommand("ping", "Checks if the bot is alive.")]
    public async Task PingCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync("Pong!");
    }
}