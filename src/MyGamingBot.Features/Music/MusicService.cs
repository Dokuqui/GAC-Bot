using System.Collections.Concurrent;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace MyGamingBot.Features.Music;

public class MusicService
{
    private readonly ConcurrentDictionary<ulong, Queue<LavalinkTrack>> _queues = new();

    public MusicService()
    {
    }

    public async Task PlayAsync(LavalinkGuildConnection conn, LavalinkTrack track)
    {
        if (conn.CurrentState.CurrentTrack == null)
        {
            await conn.PlayAsync(track);
        }
        else
        {
            var queue = GetQueue(conn.Guild.Id);
            queue.Enqueue(track);
        }
    }

    public async Task<LavalinkTrack?> SkipAsync(LavalinkGuildConnection conn)
    {
        var queue = GetQueue(conn.Guild.Id);

        if (queue.Count == 0)
        {
            await conn.StopAsync();
            return null;
        }

        var nextTrack = queue.Dequeue();
        await conn.PlayAsync(nextTrack);
        return nextTrack;
    }

    public Queue<LavalinkTrack> GetQueue(ulong guildId)
    {
        return _queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());
    }

    public async Task OnPlaybackFinished(LavalinkGuildConnection conn, TrackFinishEventArgs args)
    {
        if (args.Reason == TrackEndReason.Finished || args.Reason == TrackEndReason.LoadFailed)
        {
            var queue = GetQueue(conn.Guild.Id);
            
            if (queue.Count > 0)
            {
                var nextTrack = queue.Dequeue();
                await conn.PlayAsync(nextTrack);
            }
        }
    }
}