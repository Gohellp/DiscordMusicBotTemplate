using Discord;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;

namespace Template
{
    public record TrackData(TrackReference Reference) : ITrackQueueItem
    {

        public IUser? Owner { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; init; }
    }
}