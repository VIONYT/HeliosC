﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using HeliosC.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HeliosC
{
    public class Program
    {
        public static Task Main() => new Program().MainAsync();

        public async Task MainAsync()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json")
                .Build();

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                services
                .AddSingleton(config)
                .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = Discord.GatewayIntents.AllUnprivileged,
                    AlwaysDownloadUsers = true,
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(x => new CommandService())
                .AddSingleton<PrefixHandler>()
                )
                .Build();

            await RunAsync(host);
        }

        public async Task RunAsync(IHost host)
        {
            using IServiceScope serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var _client = provider.GetRequiredService<DiscordSocketClient>();
            var sCommands = provider.GetRequiredService<InteractionService>();
            await provider.GetRequiredService<InteractionHandler>().InitAsync();
            var config = provider.GetRequiredService<IConfigurationRoot>();
            var pCommands = provider.GetRequiredService<PrefixHandler>();
            pCommands.AddModule<PrefixModule>();
            await pCommands.InitAsync();

            _client.Log += async (LogMessage msg) => { Console.WriteLine(msg.Message); };
            sCommands.Log += async (LogMessage msg) => { Console.WriteLine(msg.Message); };

            _client.Ready += async () =>
            {
                Console.WriteLine("Logged in as " + _client.CurrentUser.Username + " in " + _client.Guilds.Count + " Guilds.");
                Console.WriteLine(_client.Guilds.ToList());
                await sCommands.RegisterCommandsGloballyAsync();
            };
            await _client.LoginAsync(TokenType.Bot, config["tokens:discord"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        } 
    } 
}