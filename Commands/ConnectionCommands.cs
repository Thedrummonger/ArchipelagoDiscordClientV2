using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ConnectionCommands
    {
        public static async Task HandleConnectCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            if (Data.socketTextChannel is null)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }

            // Ensure the channel is not already connected
            if (discordBot.ActiveSessions.ContainsKey(Data.channelId))
            {
                await command.RespondAsync("This channel is already connected to an Archipelago session.", ephemeral: true);
                return;
            }

            // Extract parameters (surely there is a better way to do this? but this is how Discord.net documentation said to do it)
            var ip = Data.GetArg("ip")?.GetValue<string>();
            var port = Data.GetArg("port")?.GetValue<long?>();
            var game = Data.GetArg("game")?.GetValue<string>();
            var name = Data.GetArg("name")?.GetValue<string>();
            var password = Data.GetArg("password")?.GetValue<string>();
            int portInt = port is null || port == default ? 38281 : (int)port;

            Console.WriteLine($"Connecting {Data.channelName} to {ip}:{port} as {name} playing {game}");
            await command.RespondAsync($"Connecting {Data.channelName} to {ip}:{port} as {name} playing {game}...");

            // Create a new session
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(ip, portInt);
                Console.WriteLine($"Trying to connect");
                //Probably doesn't need ItemsHandlingFlags.AllItems
                LoginResult result = session.TryConnectAndLogin(game, name, ItemsHandlingFlags.AllItems, new Version(0, 5, 1), ["Tracker"], null, password, true);

                if (result is LoginFailure failure)
                {
                    var errors = string.Join("\n", failure.Errors);
                    await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect to Archipelago server at {ip}:{port} as {name}.\n{errors}");
                    return;
                }

                Console.WriteLine($"Connected to Archipelago server at {ip}:{port} as {name}.");

                session.CreateArchipelagoHandlers(discordBot, Data);

                // Store the session
                discordBot.ActiveSessions[Data.channelId] = new Sessions.ActiveBotSession 
                { 
                    DiscordChannel = Data.socketTextChannel, 
                    archipelagoSession = session 
                };

                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Successfully connected channel {Data.channelName} to Archipelago server at {ip}:{port} as {name}.");
            }
            catch (Exception ex)
            {
                await command.ModifyOriginalResponseAsync(msg => msg.Content = $"Failed to connect to Archipelago server at {ip}:{port} as {name}.\n{ex.Message}");
            }
        }

        public static async Task HandleDisconnectCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            if (Data.socketTextChannel is null)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }

            // Check if the guild and channel have an active session
            if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var session))
            {
                await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                return;
            }

            await archipelagoConnectionHelpers.CleanAndCloseChannel(discordBot, Data.channelId);

            await command.RespondAsync("Successfully disconnected from the Archipelago server.");
        }
    }
}
