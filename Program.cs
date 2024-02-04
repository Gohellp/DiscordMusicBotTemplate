using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Template {
    public class Program
    {
        private static async Task Main(string[] args){
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig()
                    {
                        GatewayIntents = GatewayIntents.Guilds
                        | GatewayIntents.GuildMembers
                        | GatewayIntents.GuildMessages
                        | GatewayIntents.MessageContent

                    }));
                    services.AddHostedService<DiscordStartupService>();

                    //Commands
                    services.AddHostedService<TextBasedCommandHandlingService>();
                    services.AddSingleton<CommandService>();

                    //MusicPart
                    services.AddLavalink();
                    services.ConfigureLavalink(config =>
                    {
                        config.BaseAddress = new Uri("http://127.0.0.1:2333/");
                        config.WebSocketUri = new Uri("ws://127.0.0.1:2333/v4/websocket");
                        config.Passphrase = "urpassword";
                    });

                    services.AddHostedService<BotAudioService>();

                })
                .Build();
            await host.RunAsync();
        }
        
    }
    
}
