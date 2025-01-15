using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

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
                if (!command.Validate(discordBot, false, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
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

                var SessionInfo = new Sessions.SessionConstructor()
                {
                    ArchipelagoConnectionInfo = ConnectionData,
                    Settings = discordBot.appSettings.AppDefaultSettings
                };

                await ConnectToAPServer(command, discordBot, SessionInfo);
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
                if (!command.Validate(discordBot, false, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }

                if (!discordBot.ConnectionCache.TryGetValue(Data.textChannel!.Id, out Sessions.SessionConstructor? connectionCache) || connectionCache is null)
                {
                    await command.RespondAsync("No previous connection cached for this channel", ephemeral: true);
                    return;
                }

                await ConnectToAPServer(command, discordBot, connectionCache);
            }
        }

        public class DisconnectCommand : ICommand
        {
            public string Name => "disconnect";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Disconnect this channel from the Archipelago server").Build();

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, true, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
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

            public bool IsDebugCommand => false;

            internal async Task ConnectToAPServer(SocketSlashCommand command, DiscordBot discordBot, Sessions.SessionConstructor sessionConstructor)
            {
                var Data = command.GetCommandData();
                Console.WriteLine($"Connecting " +
                    $"{Data.channelName} to " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Name} playing " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Game}");

                await command.RespondAsync($"Connecting {Data.channelName} to " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Name} playing " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Game}...");

                // Create a new session
                try
                {
                    var session = ArchipelagoSessionFactory.CreateSession(sessionConstructor.ArchipelagoConnectionInfo!.IP, sessionConstructor.ArchipelagoConnectionInfo!.Port);

                    LoginResult result = session.TryConnectAndLogin(
                        sessionConstructor.ArchipelagoConnectionInfo!.Game,
                        sessionConstructor.ArchipelagoConnectionInfo!.Name,
                        ItemsHandlingFlags.AllItems,
                        Constants.APVersion,
                        ["TextOnly"], null,
                        sessionConstructor.ArchipelagoConnectionInfo!.Password,
                        true);

                    if (result is LoginFailure failure)
                    {
                        var errors = string.Join("\n", failure.Errors);
                        await command.ModifyOriginalResponseAsync(msg => msg.Content =
                            $"Failed to connect to Archipelago server at " +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.\n" +
                            $"{errors}");
                        return;
                    }

                    Console.WriteLine($"Connected to Archipelago server at " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.");

                    var NewSession = new Sessions.ActiveBotSession(sessionConstructor, discordBot, Data.textChannel!, session);
                    discordBot.ActiveSessions[Data.channelId] = NewSession;
                    discordBot.ConnectionCache[Data.channelId] = sessionConstructor;

                    NewSession.CreateArchipelagoHandlers();
                    _ = NewSession.MessageQueue.ProcessChannelMessages();

                    File.WriteAllText(Constants.Paths.ConnectionCache, discordBot.ConnectionCache.ToFormattedJson());

                    await command.ModifyOriginalResponseAsync(msg => msg.Content =
                        $"Successfully connected channel {Data.channelName} to Archipelago server at " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.IP}:" +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.Port} as " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.Name}.");
                }
                catch (Exception ex)
                {
                    await command.ModifyOriginalResponseAsync(msg => msg.Content =
                        $"Failed to connect to Archipelago server at " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.IP}:" +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.Port} as " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo.Name}.\n{ex.Message}");
                }
            }
        }
    }
}
