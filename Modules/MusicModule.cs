using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Tracks;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;

namespace Template
{
	[Name("Music")]
	[RequireContext(ContextType.Guild)]
	public class MusicModule(IAudioService audioService) : ModuleBase<SocketCommandContext>
	{
		private readonly IAudioService _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
		

		public enum LoopModes
		{
			One,
			All,
			Queue
		};

		[Command("play")]
		[Alias("p")]
		[Summary("Connect and play some track")]
		public async Task PlayAsync([Summary("Link to track")]string? videoLink = null)
		{
			var player = await GetPlayerAsync(true);
			if (player == null)
				return;
			

			//ResumePlayer
			if (videoLink == null)
			{
				if (player.State != PlayerState.Paused)
				{
					await ReplyAsync(embed: TrowError("Player isn't paused.\nIf you want add a track, command must include some url","MusicModule/PlayAsync/ResumePlayer"));
				}
				await player.ResumeAsync();
				await Context.Message.ReplyAsync(embed: SuccessEmbed("Player resumed"));
				return;
			}


			LavalinkTrack? track = null;

            if(Regex.IsMatch(videoLink, @"(?:.?youtu.?be)(?:.com)?")){
                track = await _audioService.Tracks
					.LoadTrackAsync(videoLink, TrackSearchMode.YouTubeMusic)
					.ConfigureAwait(false);
            }


            if (track == null)
			{
				var nullErrEmbed = new EmbedBuilder()
				{
					Color = new Color(255, 0, 0),
					Description = "😖 No results."
				};
				await ReplyAsync(embed: nullErrEmbed.Build());
				await player.DisposeAsync();
				return;
			}

			var trackData = new TrackData(new TrackReference(track))
			{
				Owner = Context.User,
				GuildId = Context.Guild.Id
			};

			TrackRepeatMode loopMode = player.RepeatMode;

			if (player.Queue.ToList().Count == 0) loopMode = TrackRepeatMode.Queue;

			player.RepeatMode = loopMode;

            int position = await player.PlayAsync(trackData, enqueue: true);

			EmbedBuilder embed = new()
			{
				Fields =
				{
					new EmbedFieldBuilder()
					{
						Name = "Track",
						Value = $"{Format.Url(track.Title, track.Uri!.ToString())}"
					},
					new EmbedFieldBuilder
					{
						Name = "Track Length",
						Value = track.Duration.ToString(@"mm\:ss"),
						IsInline = true
					}
				},
				Footer = new EmbedFooterBuilder()
				{
					IconUrl = Context.User.GetAvatarUrl(),
					Text = $"Added by {Context.User.Username} ( {Context.User.GlobalName??Context.User.Discriminator} )"
				}
			};

			if (position == 0)
			{
				embed.Description = "Playing";
			}
			else
			{
				embed.Description = "Added to queue";
				embed.AddField(new EmbedFieldBuilder{Name = "Position", Value = player.Queue.ToList().IndexOf(trackData), IsInline = true});
			}

			await Context.Message.ReplyAsync(embed: embed.Build());
			
		}

		[Command("pause")]
		[Alias("wait")]
		[Summary("Pause the playing")]
		public async Task PauseAsync()
		{
			LavalinkPlayer? player = await GetPlayerAsync();

			if (player == null)
			{
				return;
			}

			if (player.State == PlayerState.Paused)
			{
				await player.ResumeAsync();
				await Context.Message.ReplyAsync(embed: SuccessEmbed("Player unpaused!"));
			}

			if(player.CurrentTrack == null)
			{
				await Context.Message.ReplyAsync(embed: TrowError("Nothing playing!", "MusicModule/PauseAsync/PlayingCheck"));
			}

			await player.PauseAsync().ConfigureAwait(false);

			await Context.Message.ReplyAsync(embed: SuccessEmbed("Player paused"));
		}

		[Command("queue")]
		[Alias("q", "list")]
		[Summary("Sends queue")]
        public async Task SendQueueAsync()
        {
            var player = await GetPlayerAsync(false);

			if (player == null)
			{
				return;
			}
            if (player.CurrentTrack == null)
            {
                await Context.Message.ReplyAsync(embed: TrowError("Queue is empty", "MusicModule/Skip/CurrentTrackIsNull"));
                return;
            }


			string queue = $"`now` {Format.Url(player.CurrentTrack.Title, player.CurrentTrack.Uri!.ToString())}\nAdded by {((player.CurrentItem as TrackData)!.Owner as SocketUser)!.GlobalName}\n";

			for (int i = 0; i < player.Queue.Count; i++)
			{
				if (i > 10)
					return;
				TrackData track = (TrackData)player.Queue[i];
				queue += $"`{i}` {Format.Url(track.Reference.Track!.Title, track.Reference.Track!.Uri!.ToString())}\nAdded by {track.Owner!.GlobalName}\n";
			}
			EmbedBuilder embed = new()
			{
				Title = "Queue for " + Context.Guild.GetVoiceChannel(id: player.VoiceChannelId).Name,
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
				},
				Description = queue
			};
			await Context.Message.ReplyAsync(embed:embed.Build());
		}

		[Command("skip")]
		[Alias("s")]
		[Summary("Skip n tracks")]
        public async Task SkipAsync()
		{
			VoteLavalinkPlayer? player = await GetPlayerAsync(false);

			if(player == null)
			{
				return;
			}

			if(player.CurrentTrack == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("None to skip","MusicModule/Skip/CurrentTrackIsNull"));
				return;
			}


			if ((player.CurrentItem as TrackData)!.Owner as SocketUser is not IUser user)
			{
				await Context.Message.ReplyAsync(embed: TrowError("I forgor track owner, lol", "MusicModule/Skip/CurrentTrackContextIsNull"));
				return;
			}

			if (user.Id != Context.User.Id)
			{
				await player.VoteAsync(user.Id, new());
				await Context.Message.ReplyAsync(embed:SuccessEmbed("You success vote for the skip"));
				return;
			}

            EmbedBuilder embed = new()
            {
                Title = "Success",
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                },
				Description = "You skipped the track"
            };

			await player.SkipAsync();

			var _player = await GetPlayerAsync();

            if (_player!.CurrentTrack != null)
            {
                embed.AddField(
                    "Now play:",
                    $"{_player.CurrentTrack.Author} - {_player.CurrentTrack.Title}\nAdded by {user.Username}");
                
			}

            await Context.Message.ReplyAsync(embed: embed.Build());
		}

		[Command("remove")]
		[Alias("rm","del", "delete")]
		[Summary("Remove the track")]
        public async Task RemoveAsync([Summary("Index of track")]int index)
		{
			var player = await GetPlayerAsync();


            if ( player == null)
			{
				return;
			}

			TrackData trackToRemove = (TrackData)player.Queue.ElementAt(index);	

			if(trackToRemove == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I can't find this track","MusicModule/Remove"));
				return;
			}

			if (trackToRemove.Owner != Context.User)
            {
				await Context.Message.ReplyAsync(embed:TrowError("You can't remove this track", "MusicModule/RemoveAsync"));
				return;
			}

			await player.Queue.RemoveAsync(trackToRemove);

			await Context.Message.ReplyAsync(embed:SuccessEmbed("Track "+trackToRemove.Reference.Track!.Author+" - "+trackToRemove.Reference.Track.Title+" was removed"));
			
		}

		[Command("nowplaying")]
		[Alias("np", "track", "song")]
		[Summary("Sends track info")]
		public async Task NowPlayingAsync()
        {
			var player = await GetPlayerAsync(false);
			if (player == null)
			{
				return;
			}

			TrackData? track = player.CurrentItem as TrackData;

			if(track == null || player.State is PlayerState.NotPlaying or PlayerState.Paused)
			{
				await Context.Message.ReplyAsync(embed:TrowError("Nothing playing now", "MusicModule/NowPlayingAsync/CurrentTrackIsNul"));
				return;
			}

			IUser? owner = track.Owner;

			if (owner == null)
			{
				await Context.Message.ReplyAsync(embed:TrowError("I forgor track owner, lol", "MusicModule/Skip/CurrentTrackContextIsNull"));
				return;
			}

			EmbedBuilder embed = new()
			{
                Title = "Information",
                Color = new Color(0, 255, 255),
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Client.CurrentUser.Username,
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                }
            };
			embed.AddField(
                    "Now play:",
                    $"{track.Reference.Track!.Author} - {track.Reference.Track.Title}\nAdded by {owner.Username}");
            embed.Url = track.Reference.Track.Uri!.ToString();

            await Context.Message.ReplyAsync(embed:embed.Build());
        }

		[Command("stop")]
		[Alias("leave", "die", "disconnect")]
		[Summary("Stop playing and leave from channel")]
		public async Task StopAsync()
		{
			var player = await GetPlayerAsync(false);

			if (player == null)
			{
				return;
			}

			await player.DisposeAsync();
			await player.DisposeAsync();
			await ReplyAsync(embed:SuccessEmbed("Disconnected"));
		}

		[Command("loop")]
		[Alias("l")]
		[Summary("Loop playing")]//Placeholder
        public async Task LoopAsync([Summary("Loop mode")]LoopModes mode)
        {

			SocketGuildUser user = (SocketGuildUser)Context.User;

			var player = await GetPlayerAsync(false);

			if (player == null)
			{
				return;
			}
			
			switch(mode)
			{
				case LoopModes.One:
					player.RepeatMode = TrackRepeatMode.Track;
					break;
				case LoopModes.All or LoopModes.Queue:
					player.RepeatMode = TrackRepeatMode.Queue;
					break;
				default:
					if (player.RepeatMode == TrackRepeatMode.None)
						player.RepeatMode = TrackRepeatMode.Queue;
					else
						player.RepeatMode = TrackRepeatMode.None;
					break;
			}

			await Context.Message.ReplyAsync(embed:SuccessEmbed("Loop mode sets to "+mode));
        }

		//Some Private Functions
		private async ValueTask<VoteLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
		{
			var channelBehavior = connectToVoiceChannel
				? PlayerChannelBehavior.Join
				: PlayerChannelBehavior.None;

			var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);
			var options = new VoteLavalinkPlayerOptions
			{
				SelfDeaf = true,
				InitialVolume = 0.45f
			};
			var user = Context.User as IGuildUser;

			//Something going wrong here
			var result = await _audioService.Players
                    .RetrieveAsync(Context.Guild.Id,
                        user!.VoiceChannel.Id,
					PlayerFactory.Vote,
					Options.Create(options),
					retrieveOptions);
			//End of something wrong

			if (!result.IsSuccess)
			{
				var errorMessage = result.Status switch
				{
					PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
					PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
					_ => "Unknown error.",
				};
				await Context.Message.ReplyAsync(embed: TrowError(errorMessage,"GetPlayerAsync\nFUCK U StackTrace!!!!"));
				return null;
			}

			return result.Player;
		}

		private Embed TrowError(string _description, string _blockTrowedError)
		{
			var embed = new EmbedBuilder()
			{
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
				},
				Title = "ERROR",
				Color = Color.Red,
				Description = _description
			};
			embed.AddField(
				name: "Block trowed error",
				value: _blockTrowedError
			);

			return embed.Build();
		}

		private Embed SuccessEmbed(string _description)
		{
			EmbedBuilder embed = new()
			{
				Title = "Success",
				Color = Color.Green,
				Author = new EmbedAuthorBuilder()
				{
					Name = Context.Client.CurrentUser.Username,
					IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
				},
				Description = _description
			};

			return embed.Build();
		}
	}
}
