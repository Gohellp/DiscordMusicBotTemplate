using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players.Vote;
using Microsoft.Extensions.Hosting;

namespace Template
{
    internal class BotAudioService(IAudioService _audioService, DiscordSocketClient _client) : BackgroundService
    {
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _audioService.TrackStarted += async (_, args) =>
            {
                var player = args.Player as IVoteLavalinkPlayer;

                var track = player!.CurrentItem as TrackData;

                ITextChannel channel = (ITextChannel)await _client.GetChannelAsync(track!.ChannelId);

                await channel.SendMessageAsync($"Now playing: {args.Track.Uri}");
            };

            return Task.CompletedTask;
        }
    }
}