using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Template
{
	public class TextBasedCommandHandlingService(
		DiscordSocketClient _discord,
		CommandService _commands,
		IServiceProvider _services) : IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_discord.MessageReceived += HandleCommandAsync;

			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.CompletedTask;
		}

		private Task HandleCommandAsync(SocketMessage messageParam)
		{
			_ = Task.Run(async () =>
			{
				if (messageParam is not SocketUserMessage message) return;

				// Create a number to track where the prefix ends and the command begins
				int argPos = 0;

				// Create a WebSocket-based command context based on the message
				var context = new SocketCommandContext(_discord, message);

				var pref = "-";
				// Determine if the message is a command based on the prefix and make sure no bots trigger commands
				if (!(message.HasStringPrefix(pref, ref argPos) ||
					message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) ||
					message.Author.IsBot)
					return;

				// Execute the command with the command context we just
				// created, along with the service provider for precondition checks.
				await _commands.ExecuteAsync(
					context: context,
					argPos: argPos,
					services: _services);
			});
			return Task.CompletedTask;
		}
	}
}