using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
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
                    .AddOption("address", ApplicationCommandOptionType.String, "Server address. formatted {ip}:{port}", true)
                    .AddOption("name", ApplicationCommandOptionType.String, "Slot Name of the connecting player", true)
                    .AddOption("password", ApplicationCommandOptionType.String, "Server password (optional)", false).Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, false, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                var IPArg = Data.GetArg("address")?.GetValue<string>();
                var NameArg = Data.GetArg("name")?.GetValue<string>();
                var PasswordArg = Data.GetArg("password")?.GetValue<string>();
                var (Ip, Port) = NetworkHelpers.ParseIpAddress(IPArg);
                if (Ip is null || string.IsNullOrWhiteSpace(NameArg))
                {
                    await command.RespondAsync("Connection args invalid.", ephemeral: true);
                    return;
                }
                var ConnectionData = new Sessions.ArchipelagoConnectionInfo
                {
                    IP = Ip,
                    Port = Port,
                    Name = NameArg,
                    Password = PasswordArg
                };

                var SessionInfo = new Sessions.SessionConstructor
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
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription(result).WithColor(Color.Red).Build(), ephemeral: true);
                    return;
                }

                if (!discordBot.ConnectionCache.TryGetValue(Data.TextChannel!.Id, out Sessions.SessionConstructor? connectionCache) || connectionCache is null)
                {
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription("No previous connection cached for this channel").WithColor(Color.Red).Build(), ephemeral: true);
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
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription(result).WithColor(Color.Red).Build(), ephemeral: true);
                    return;
                }
                await command.RespondAsync(embed: new EmbedBuilder().WithDescription("Disconnecting from the Archipelago server.").Build());

                await ArchipelagoConnectionHelpers.CleanAndCloseChannel(discordBot, Data.ChannelId);

                await command.ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
                    .WithDescription("Successfully disconnected from the Archipelago server.")
                    .WithColor(Color.Orange).Build());
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

                await command.RespondAsync(embed: new EmbedBuilder().WithDescription($"Connecting {Data.ChannelName} to " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}").Build());

                // Create a new session
                try
                {
                    var session = ArchipelagoSessionFactory.CreateSession(sessionConstructor.ArchipelagoConnectionInfo!.IP, sessionConstructor.ArchipelagoConnectionInfo!.Port);

                    LoginResult result = session.TryConnectAndLogin(
                        null, //Game is not needed since we connect with the TextOnly Tag
                        sessionConstructor.ArchipelagoConnectionInfo!.Name,
                        ItemsHandlingFlags.AllItems,
                        Constants.APVersion,
                        ["TextOnly"], null,
                        sessionConstructor.ArchipelagoConnectionInfo!.Password,
                        true);

                    if (result is LoginFailure failure)
                    {
                        var errors = string.Join("\n", failure.Errors);
                        await command.ModifyOriginalResponseAsync(msg => msg.Embed = new EmbedBuilder().WithDescription(
                            $"Failed to connect to Archipelago server at " +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                            $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.\n" +
                            $"{errors}"
                            ).WithColor(Color.Red).Build());
                        return;
                    }

                    var NewSession = new Sessions.ActiveBotSession(sessionConstructor, discordBot, Data.TextChannel!, session);
                    discordBot.ActiveSessions[Data.ChannelId] = NewSession;

                    discordBot.UpdateConnectionCache(Data.ChannelId, sessionConstructor);

                    NewSession.CreateArchipelagoHandlers();
                    _ = NewSession.MessageQueue.ProcessChannelMessages();

                    var SuccessMessage = $"Successfully connected channel {Data.ChannelName} to Archipelago server at " +
                        $"{NewSession.ArchipelagoSession.Socket.Uri.Host}:" +
                        $"{NewSession.ArchipelagoSession.Socket.Uri.Port} as " +
                        $"{NewSession.ArchipelagoSession.Players.ActivePlayer.Name} playing " +
                        $"{NewSession.ArchipelagoSession.Players.ActivePlayer.Game}.";

                    Console.WriteLine(SuccessMessage);
                    await command.ModifyOriginalResponseAsync(msg => msg.Embed = new EmbedBuilder()
                        .WithDescription(SuccessMessage).WithColor(Color.Green).Build());
                }
                catch (Exception ex)
                {
                    await command.ModifyOriginalResponseAsync(msg => msg.Embed = new EmbedBuilder().WithDescription(
                        $"Failed to connect to Archipelago server at " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.\n" +
                        $"{ex}"
                        ).WithColor(Color.Red).Build());
                }
            }
        }
    }
}
