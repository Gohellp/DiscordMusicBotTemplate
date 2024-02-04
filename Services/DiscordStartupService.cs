using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Template
{
    public class DiscordStartupService(
            IConfiguration _config,
            DiscordSocketClient _discord,
            ILogger<DiscordSocketClient> _logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
            await _discord.LoginAsync(TokenType.Bot, _config["DISCORD_TOKEN"]);
            await _discord.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discord.LogoutAsync();
            await _discord.StopAsync();
        }
    }
}