using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Lavalink;
using System.Text;

namespace MyGamingBot.Features.Music;

[SlashCommandGroup("music", "Commands to play music.")]
public class MusicCommands : ApplicationCommandModule
{
    private readonly LavalinkExtension _lavalink;
    private readonly MusicService _musicService;

    public MusicCommands(LavalinkExtension lavalink, MusicService musicService)
    {
        _lavalink = lavalink;
        _musicService = musicService;
    }

    [SlashCommand("join", "Join your voice channel.")]
    public async Task Join(InteractionContext ctx)
    {
        var userVc = ctx.Member?.VoiceState?.Channel;
        if (userVc == null)
        {
            await ctx.CreateResponseAsync("❌ You need to be in a voice channel first!");
            return;
        }

        var node = _lavalink.GetIdealNodeConnection();
        if (node == null)
        {
            await ctx.CreateResponseAsync("❌ Lavalink is not connected.");
            return;
        }

        var conn = await node.ConnectAsync(userVc);
        
        conn.PlaybackFinished += _musicService.OnPlaybackFinished;

        await ctx.CreateResponseAsync($"🔊 Joined {userVc.Name}!");
    }

    [SlashCommand("leave", "Leave the voice channel.")]
    public async Task Leave(InteractionContext ctx)
    {
        var node = _lavalink.GetIdealNodeConnection();
        var conn = node?.GetGuildConnection(ctx.Guild);

        if (conn == null)
        {
            await ctx.CreateResponseAsync("❌ I'm not in a voice channel.");
            return;
        }

        conn.PlaybackFinished -= _musicService.OnPlaybackFinished;

        _musicService.GetQueue(ctx.Guild.Id).Clear();

        await conn.DisconnectAsync();
        await ctx.CreateResponseAsync("👋 Disconnected.");
    }

    [SlashCommand("play", "Play a song.")]
    public async Task Play(InteractionContext ctx, [Option("query", "The song name or URL")] string query)
    {
        await ctx.DeferAsync();

        var userVc = ctx.Member?.VoiceState?.Channel;
        var node = _lavalink.GetIdealNodeConnection();
        var conn = node?.GetGuildConnection(ctx.Guild);

        if (conn == null)
        {
            if (userVc == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("❌ Join a voice channel first!"));
                return;
            }
            conn = await node.ConnectAsync(userVc);
            conn.PlaybackFinished += _musicService.OnPlaybackFinished;
        }

        LavalinkLoadResult loadResult;

        if (Uri.TryCreate(query, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            loadResult = await node.Rest.GetTracksAsync(uriResult);
        }
        else
        {
            loadResult = await node.Rest.GetTracksAsync(query, LavalinkSearchType.SoundCloud);
        }

        if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"❌ Could not find '{query}'."));
            return;
        }

        var track = loadResult.Tracks.First();

        await _musicService.PlayAsync(conn, track);

        string status = conn.CurrentState.CurrentTrack == track ? "Now Playing" : "Added to Queue";

        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🎶 {status}")
            .WithDescription($"[{track.Title}]({track.Uri})")
            .WithColor(DiscordColor.Purple)
            .AddField("Duration", track.Length.ToString(@"mm\:ss"));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [SlashCommand("skip", "Skip the current song.")]
    public async Task Skip(InteractionContext ctx)
    {
        var conn = GetConnection(ctx);
        if (conn == null) { await ctx.CreateResponseAsync("❌ Not connected."); return; }

        var nextTrack = await _musicService.SkipAsync(conn);

        if (nextTrack != null)
        {
            await ctx.CreateResponseAsync($"⏭️ Skipped! Now playing: **{nextTrack.Title}**");
        }
        else
        {
            await ctx.CreateResponseAsync("⏹️ Skipped. Queue is empty, stopping music.");
        }
    }

    [SlashCommand("queue", "Show the upcoming songs.")]
    public async Task Queue(InteractionContext ctx)
    {
        var queue = _musicService.GetQueue(ctx.Guild.Id);

        if (queue.Count == 0)
        {
            await ctx.CreateResponseAsync("The queue is empty.");
            return;
        }

        var sb = new StringBuilder();
        int i = 1;
        foreach (var track in queue.Take(10))
        {
            sb.AppendLine($"{i}. **{track.Title}** ({track.Length:mm\\:ss})");
            i++;
        }

        if (queue.Count > 10)
        {
            sb.AppendLine($"...and {queue.Count - 10} more.");
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📜 Music Queue")
            .WithDescription(sb.ToString())
            .WithColor(DiscordColor.Purple);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }

    private LavalinkGuildConnection? GetConnection(InteractionContext ctx)
    {
        var node = _lavalink.GetIdealNodeConnection();
        return node?.GetGuildConnection(ctx.Guild);
    }
}