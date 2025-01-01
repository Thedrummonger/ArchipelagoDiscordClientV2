using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using TDMUtils;
using Discord;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ConnectionCommands
    {
        public class ConnectCommand : IGenericConnectCommand
        {
            public override string Name => "connect";
            public override SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Connect this channel to an Archipelago server")
                    .AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
                    .AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
                    .AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
                    .AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
                    .AddOption("password", ApplicationCommandOptionType.String, "Optional password", false).Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
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

                var PortParam = Data.GetArg("port")?.GetValue<long?>();
                int ParsedPort = PortParam is null || PortParam == default || PortParam < 1 ? 38281 : (int)PortParam;
                var ConnectionData = new Sessions.ArchipelagoConnectionInfo
                {
                    IP = Data.GetArg("ip")?.GetValue<string>(),
                    Port = ParsedPort,
                    Game = Data.GetArg("game")?.GetValue<string>(),
                    Name = Data.GetArg("name")?.GetValue<string>(),
                    Password = Data.GetArg("password")?.GetValue<string>()
                };

                if (ConnectionData.IP is null || ConnectionData.Game is null || ConnectionData.Name is null)
                {
                    await command.RespondAsync("Connection args invalid.", ephemeral: true);
                    return;
                }

                await ConnectToAPServer(command, discordBot, Data, ConnectionData);
            }
        }

        public class ReConnectCommand : IGenericConnectCommand
        {
            public override string Name => "reconnect";

            public override SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Connects to the server using the last known working connection info.").Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
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

                if (!discordBot.ConnectionCache.TryGetValue(Data.socketTextChannel.Id, out Sessions.ArchipelagoConnectionInfo? connectionInfo) || connectionInfo is null)
                {
                    await command.RespondAsync("No previous connection cached for this channel", ephemeral: true);
                    return;
                }

                await ConnectToAPServer(command, discordBot, Data, connectionInfo);
            }
        }

        public class DisconnectCommand : ICommand
        {
            public string Name => "disconnect";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Disconnect this channel from the Archipelago server").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
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

        public abstract class IGenericConnectCommand : ICommand
        {
            public abstract string Name { get; }
            public abstract SlashCommandProperties Properties { get; }
            public abstract Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot);

            internal async Task ConnectToAPServer(SocketSlashCommand command, DiscordBot discordBot, CommandData.CommandDataModel Data, Sessions.ArchipelagoConnectionInfo ConnectionData)
            {
                Console.WriteLine($"Connecting {Data.channelName} to {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name} playing {ConnectionData.Game}");
                await command.RespondAsync($"Connecting {Data.channelName} to {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name} playing {ConnectionData.Game}...");

                // Create a new session
                try
                {
                    var session = ArchipelagoSessionFactory.CreateSession(ConnectionData.IP, ConnectionData.Port);
                    Console.WriteLine($"Trying to connect");

                    LoginResult result = session.TryConnectAndLogin(
                        ConnectionData.Game,
                        ConnectionData.Name,
                        ItemsHandlingFlags.AllItems, //Probably doesn't need ItemsHandlingFlags.AllItems
                        new Version(0, 5, 1),
                        ["Tracker"], null,
                        ConnectionData.Password,
                        true);

                    if (result is LoginFailure failure)
                    {
                        var errors = string.Join("\n", failure.Errors);
                        await command.ModifyOriginalResponseAsync(msg => msg.Content =
                            $"Failed to connect to Archipelago server at {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name}.\n{errors}");
                        return;
                    }

                    Console.WriteLine($"Connected to Archipelago server at {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name}.");

                    // Store the session
                    discordBot.ActiveSessions[Data.channelId] = new Sessions.ActiveBotSession
                    {
                        DiscordChannel = Data.socketTextChannel!,
                        archipelagoSession = session
                    };
                    discordBot.ConnectionCache[Data.channelId] = ConnectionData;

                    session.CreateArchipelagoHandlers(discordBot, discordBot.ActiveSessions[Data.channelId]);

                    File.WriteAllText(Constants.Paths.ConnectionCache, discordBot.ConnectionCache.ToFormattedJson());

                    await command.ModifyOriginalResponseAsync(msg => msg.Content =
                        $"Successfully connected channel {Data.channelName} to Archipelago server at {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name}.");
                }
                catch (Exception ex)
                {
                    await command.ModifyOriginalResponseAsync(msg => msg.Content =
                        $"Failed to connect to Archipelago server at {ConnectionData.IP}:{ConnectionData.Port} as {ConnectionData.Name}.\n{ex.Message}");
                }
            }
        }
    }
}
